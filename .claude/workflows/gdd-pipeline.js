export const meta = {
  name: 'gdd-pipeline',
  description: 'Brief, draft, review and fix GDDs in batches (unity-harness game-design / game-design-review)',
  whenToUse: 'mode "brief": collect major decisions per system before design. mode "design": systems[].kind new | continue | revise | review; independent review, fix loop, consistency check, single-writer sync of systems-index and registry. args: {mode, date, maxFixRounds? (1), maxContinueRounds? (2), maxAgents? (40), maxGddLines? (500), maxOutputTokens?, models? (override auto routing per stage: brief/draft/continue/check/review/fix/consistency/sync, value a model or "inherit"), modelTiers? ({low:haiku, mid:sonnet, high:opus}); per system: complexity? low|normal|high, highRisk?, models?, registryChanges?: [user-approved registry edits], systems: [{name, kind, path?, after?, decisions?, fixList?}]}. Review round 2+ is a verification pass, not a full review.',
  phases: [
    { title: 'Brief', detail: 'one game-designer per system lists major decisions (read-only)' },
    { title: 'Draft', detail: 'kind new / continue / revise (kind review skips this)' },
    { title: 'Complete', detail: 'completeness check + continuation writers' },
    { title: 'Review', detail: 'fresh reviewer per round, lean game-design-review' },
    { title: 'Fix', detail: 'fresh fixer for non-user blocking findings' },
    { title: 'Consistency', detail: 'cross-check the batch and upstream GDDs' },
    { title: 'Sync', detail: 'only writer of systems-index.md, entities.yaml and GDD status lines' },
  ],
}

const A = args || {}
const MODE = A.mode
const DATE = A.date
const SKILLS = A.skillsRoot || 'C:/Users/Admin/.claude/plugins/cache/unity-harness-loop/unity-harness/0.9.1/skills'
const MAX_FIX = typeof A.maxFixRounds === 'number' ? A.maxFixRounds : 1
const MAX_LINES = typeof A.maxGddLines === 'number' ? A.maxGddLines : 500
const TIER = Object.assign({ low: 'haiku', mid: 'sonnet', high: 'opus' }, A.modelTiers || {})
const isHard = s => !!s && (s.complexity === 'high' || s.highRisk === true)
const pickModel = (stage, s, ctx) => {
  const c = ctx || {}
  const forced = (s && s.models && s.models[stage]) || (A.models && A.models[stage])
  if (forced) return forced === 'inherit' ? null : forced
  if (s && s.complexity === 'low' && stage !== 'check') return TIER.mid
  switch (stage) {
    case 'check': return TIER.low
    case 'brief': return isHard(s) ? TIER.high : TIER.mid
    case 'draft': return isHard(s) || ((s.kind || 'new') !== 'revise' && (s.decisions || []).length >= 6) ? TIER.high : TIER.mid
    case 'continue': return TIER.mid
    case 'review': return c.round > 1 ? TIER.mid : (isHard(s) ? TIER.high : TIER.mid)
    case 'fix': return c.escalate || isHard(s) ? TIER.high : TIER.mid
    case 'consistency': return SYSTEMS.length >= 4 ? TIER.high : TIER.mid
    case 'sync': return TIER.mid
    default: return TIER.mid
  }
}
const modelUse = {}
const routed = (stage, s, ctx, opts) => {
  const m = pickModel(stage, s, ctx)
  const key = m || 'inherit'
  modelUse[key] = (modelUse[key] || 0) + 1
  return Object.assign({}, opts, m ? { model: m } : {}, { label: `${opts.label} [${key}]` })
}
const OUT_BUDGET = typeof A.maxOutputTokens === 'number' ? A.maxOutputTokens : null
const overBudget = () => OUT_BUDGET !== null && budget.spent() >= OUT_BUDGET
const SYSTEMS = Array.isArray(A.systems) ? A.systems : []

if (!MODE || !DATE || !SYSTEMS.length) {
  throw new Error('gdd-pipeline needs args {mode: "brief"|"design", date: "YYYY-MM-DD", systems: [...]}')
}

const kebab = n => n.toLowerCase().replace(/&/g, ' and ').replace(/[^a-z0-9]+/g, '-').replace(/^-|-$/g, '')
const gddPath = s => s.path || `Docs/design/gdd/${kebab(s.name)}.md`
const reviewPath = s => `Docs/design/gdd/reviews/${kebab(s.name)}-review-${DATE}.md`

const PROJECT = `Project: Unity game "Cozy Gachapon Room", repo root = current working directory.
Studio settings: workflow standard, reviewMode lean, docsDensity balanced, language vi (write documents in Vietnamese; skill/file names stay English), automation replaced by this workflow (no questions to the user).
Engine: Unity 6000.3.16f1, URP 2D, new Input System, platforms WebGL (Poki/CrazyGames) + Android + iOS, budgets 60 FPS, <=100 draw calls, <=1024 MB. Test asmdef CozyGachapon.Tests.EditMode.
Key files: Docs/design/gdd/game-concept.md (pillars, MVP), Docs/design/gdd/systems-index.md (systems, layers, deps), Docs/design/registry/entities.yaml (locked values: never contradict silently), Docs/design/gdd/*.md (existing GDDs), Docs/design/gdd/reviews/ (review reports).
Never ask the user anything: replace every user question of a skill as instructed below.`

const AUTHOR_SKILL = `Skill: game-design. Read ${SKILLS}/game-design/SKILL.md and ${SKILLS}/game-design/references/gdd-template.md; read references/sections.md only for the guidance of sections you actually write. Do not read post-design.md or any other skill.`
const REVIEW_SKILL = `Skill: game-design-review. Read ${SKILLS}/game-design-review/SKILL.md and ${SKILLS}/game-design-review/references/review-report.md only. Do not read other skills.`

const READ_ECONOMY = `Token economy (every file you read is re-sent with each later tool call, so reading is the main cost):
- Read each file at most once; never re-read a file after editing it (you already know what you wrote).
- The document you own or review: read it once in full. Every OTHER document (other GDDs, game-concept.md, systems-index.md, entities.yaml, review reports): do NOT read it whole. Grep it (-n, a few lines of context) for the names you need, then Read only the matching ranges with offset/limit (typically its Dependencies, Formulas, Tuning Knobs, Edge Cases, Open Questions).
- Aim for at most ~15 tool calls in total.`

const RULES = `Hard rules:
- Never run git commit/push. Never create or edit files outside Docs/.
- Never edit Docs/design/gdd/systems-index.md, Docs/design/registry/entities.yaml or Docs/production/session-state/active.md: a separate Sync step is their only writer. Report what they need instead.
- Every number in a design is a tuning knob listed in the Tuning Knobs section (Asset.field, current value, safe range); formulas reference knobs, never bare literals. Engine constants are marked "constant" with a reason.
- Every acceptance criterion is measurable and tagged [EditMode], [Play mode], [Screenshot] or [Perf], in GIVEN/WHEN/THEN form.
- Do not spawn subagents.
- Length: a GDD targets at most ${MAX_LINES} lines; prefer tables to prose, never repeat an explanation, keep any revision note to one line. A fix or revision must not grow an existing document by more than about 10%: when you add, cut redundant prose nearby.`

const DECISION = {
  type: 'object',
  properties: {
    id: { type: 'string' },
    title: { type: 'string' },
    question: { type: 'string', description: 'the question to put to the user, in Vietnamese' },
    whyMajor: { type: 'string', description: 'naming / scope / mutually exclusive direction / hard to reverse' },
    hardToReverse: { type: 'boolean' },
    options: {
      type: 'array', minItems: 2, maxItems: 4,
      items: {
        type: 'object',
        properties: { label: { type: 'string' }, tradeoff: { type: 'string' } },
        required: ['label', 'tradeoff'],
      },
    },
    recommended: { type: 'string', description: 'label of the recommended option' },
    numbers: { type: 'string', description: 'comparison numbers if the choice is numeric, else empty' },
  },
  required: ['id', 'title', 'question', 'whyMajor', 'hardToReverse', 'options', 'recommended'],
}

const BRIEF_SCHEMA = {
  type: 'object',
  properties: {
    system: { type: 'string' },
    layer: { type: 'string' },
    priority: { type: 'string' },
    scopeSummary: { type: 'string' },
    upstreamGddsRead: { type: 'array', items: { type: 'string' } },
    registryFactsRespected: { type: 'array', items: { type: 'string' } },
    decisions: { type: 'array', maxItems: 6, items: DECISION },
    minorChoices: {
      type: 'array',
      items: {
        type: 'object',
        properties: { topic: { type: 'string' }, picked: { type: 'string' }, why: { type: 'string' } },
        required: ['topic', 'picked', 'why'],
      },
    },
    risks: { type: 'array', items: { type: 'string' } },
  },
  required: ['system', 'scopeSummary', 'decisions', 'minorChoices'],
}

const FINDING = {
  type: 'object',
  properties: {
    id: { type: 'string' },
    severity: { type: 'string', enum: ['blocking', 'recommended'] },
    needsUser: { type: 'boolean', description: 'true only if fixing it requires a major/irreversible design decision the user must make' },
    section: { type: 'string' },
    summary: { type: 'string' },
    suggestedFix: { type: 'string' },
  },
  required: ['id', 'severity', 'needsUser', 'section', 'summary', 'suggestedFix'],
}

const REVIEW_SCHEMA = {
  type: 'object',
  properties: {
    verdict: { type: 'string', enum: ['APPROVED', 'NEEDS REVISION', 'MAJOR REVISION NEEDED', 'NOT ASSESSED'] },
    reportPath: { type: 'string' },
    priorBlockingAddressed: { type: 'array', items: { type: 'string' } },
    findings: { type: 'array', items: FINDING },
  },
  required: ['verdict', 'reportPath', 'findings'],
}

const CONSISTENCY_SCHEMA = {
  type: 'object',
  properties: {
    findings: {
      type: 'array',
      items: {
        type: 'object',
        properties: {
          severity: { type: 'string', enum: ['blocking', 'recommended'] },
          kind: { type: 'string', enum: ['dependency-direction', 'interface-mismatch', 'registry-conflict', 'contradiction', 'other'] },
          systems: { type: 'array', items: { type: 'string' } },
          files: { type: 'array', items: { type: 'string' } },
          summary: { type: 'string' },
          suggestedFix: { type: 'string' },
        },
        required: ['severity', 'kind', 'systems', 'summary', 'suggestedFix'],
      },
    },
  },
  required: ['findings'],
}

const SYNC_SCHEMA = {
  type: 'object',
  properties: {
    indexRowsUpdated: { type: 'array', items: { type: 'string' } },
    headersUpdated: { type: 'array', items: { type: 'string' } },
    registryAdded: { type: 'array', items: { type: 'string' } },
    registryReferencedByUpdated: { type: 'array', items: { type: 'string' } },
    registryConflicts: { type: 'array', items: { type: 'string' } },
    indexNotesApplied: { type: 'array', items: { type: 'string' } },
    indexNotesSkipped: { type: 'array', items: { type: 'string' } },
  },
  required: ['indexRowsUpdated', 'headersUpdated', 'registryAdded', 'registryConflicts'],
}

if (MODE === 'brief') {
  phase('Brief')
  const briefs = await parallel(SYSTEMS.map(s => () => agent(
`${PROJECT}

Task: pre-design brief for the system "${s.name}" (date ${DATE}). READ ONLY: do not create or edit any file.
Read: game-concept.md, systems-index.md (this system's row, its priority, layer, Depends On, and who depends on it), entities.yaml, the relevant parts of the GDDs of its upstream and downstream systems if they exist in Docs/design/gdd/, and ${SKILLS}/game-design/SKILL.md (phase 2: context + Unity feasibility is what you are doing, without asking anything).\n${READ_ECONOMY}

Return the MAJOR decisions the user must settle before this GDD can be written: naming, scope boundaries, mutually exclusive directions, anything costly to reverse (data formats, ownership of values shared with other systems, monetization or pillar trade-offs). At most 6. Each has 2-4 options with concrete trade-offs and one recommendation; when the choice is numeric include a short comparison in "numbers". Write questions and trade-offs in Vietnamese.
Minor decisions (defaults, internal structure, wording): choose yourself and list them in minorChoices with the reason.
Respect registry values and existing approved/in-review GDDs; if a decision would change a locked value, say so in the decision's whyMajor.

${RULES}`,
    routed('brief', s, null, { label: `brief:${s.name}`, phase: 'Brief', agentType: 'unity-harness:game-designer', schema: BRIEF_SCHEMA }),
  )))
  const missing = SYSTEMS.filter((s, i) => !briefs[i]).map(s => s.name)
  if (missing.length) log(`No brief returned for: ${missing.join(', ')}`)
  log(`models used: ${JSON.stringify(modelUse)}`)
  return { mode: 'brief', date: DATE, briefs: briefs.filter(Boolean), missing, modelUse }
}

if (MODE !== 'design') throw new Error(`unknown mode ${MODE}`)

const MAX_CONTINUE = typeof A.maxContinueRounds === 'number' ? A.maxContinueRounds : 2
const REGISTRY_CHANGES = Array.isArray(A.registryChanges) ? A.registryChanges : []

const WRITE_STYLE = `Writing style: a new document is built in 3-4 large writes (for example: skeleton + Summary/Overview/Detailed Design; then Formulas + Edge Cases; then Dependencies + Tuning Knobs; then the remaining sections), so finished parts are on disk if you are interrupted but tool calls stay few. A revision groups nearby changes into as few Edit calls as practical; never rewrite a whole existing file in one Write. When done, reply with at most 10 lines: what you wrote or changed, any "Assumed:" choices, and any NEEDS-USER question you added.`

const decisionsBlock = s => `User decisions to apply exactly:
${JSON.stringify(s.decisions || [], null, 2)}`

const choiceRules = `Choices you must make yourself: a minor one → pick the recommended option and record it in Open Questions as "Assumed: <choice> (why)". A major one not covered by the user decisions (naming, scope, mutually exclusive direction, hard to reverse) → do not settle it: add an Open Question tagged NEEDS-USER with 2-4 options and a recommendation, and write the dependent text as provisional.`

const sectionsSpec = `Sections (template ${SKILLS}/game-design/references/gdd-template.md, Vietnamese, density balanced): Summary with Quick reference, Overview, Detailed Design (Core Rules, States and Transitions, Interactions with Other Systems), Formulas (whenever there is any numeric rule; the template's variable table, output range and worked example per formula), Edge Cases, Dependencies (bidirectional, with interface per row), Tuning Knobs (project rule: every number), Visual/Audio Requirements, Game Feel and UI Requirements where they apply (write "Không áp dụng — <lý do>" otherwise), Cross-References, Acceptance Criteria, Open Questions. Write Formulas and Acceptance Criteria with specialist rigor (min/max inputs, one criterion per rule and per formula). No specialist subagents.`

const authorPrompt = s => {
  const path = gddPath(s)
  const kind = s.kind || 'new'
  const common = `${AUTHOR_SKILL}
Context to look up (per the token economy rules): game-concept.md pillars, this system's systems-index row, entities.yaml entries it uses, and the interface parts of the GDDs this system depends on or that depend on it (some are written in this same batch; match their event names, payload fields, knob names and ownership exactly).
${READ_ECONOMY}
Header: "> **Status**: In Review" and "Last Updated: ${DATE}" (the Sync step sets the final status word). Skip the skill's phase-5 writes to systems-index.md, entities.yaml and session state.`
  if (kind === 'revise') {
    return `${PROJECT}

Task: REVISE the existing GDD ${path} for "${s.name}" (date ${DATE}). Apply ONLY the items in the fix list; keep every other section and sentence unchanged. If an item is already satisfied by the current text, leave it and say so in your reply.
${common}
${decisionsBlock(s)}
Fix list:
${JSON.stringify(s.fixList || [], null, 2)}
${choiceRules}
${WRITE_STYLE}

${RULES}`
  }
  if (kind === 'continue') {
    return `${PROJECT}

Task: COMPLETE the existing, partly written GDD ${path} for "${s.name}" (date ${DATE}). Keep every section that already has real content; write every section that is missing, empty or still "[To be designed]". Also apply the fix list to the existing content.
${common}
${sectionsSpec}
${decisionsBlock(s)}
Fix list:
${JSON.stringify(s.fixList || [], null, 2)}
${choiceRules}
${WRITE_STYLE}

${RULES}`
  }
  return `${PROJECT}

Task: WRITE the new GDD ${path} for "${s.name}" (date ${DATE}) following the game-design skill (phases 2-5) with every user question replaced as below.
${common}
${sectionsSpec}
${decisionsBlock(s)}
${choiceRules}
${WRITE_STYLE}

${RULES}`
}

const COMPLETENESS_SCHEMA = {
  type: 'object',
  properties: {
    complete: { type: 'boolean' },
    missingSections: { type: 'array', items: { type: 'string' } },
    placeholderCount: { type: 'integer' },
    knobsUsedButNotListed: { type: 'array', items: { type: 'string' } },
  },
  required: ['complete', 'missingSections', 'placeholderCount'],
}

const completenessPrompt = s => `READ ONLY check of ${gddPath(s)} (edit nothing, do not read the whole file). Use Grep on that file only: pattern "^## " (-n) to list section headings, and pattern "To be designed" (-n) for placeholders. Required "## " sections: Summary, Overview, Detailed Design, Formulas (if any numeric rule), Edge Cases, Dependencies, Tuning Knobs, Visual/Audio Requirements, Game Feel, UI Requirements, Cross-References, Acceptance Criteria, Open Questions. A section is missing if its heading is absent, if a placeholder line falls inside it, or if the next heading follows within 2 lines. Return knobsUsedButNotListed as an empty list. complete = no missing section and no placeholder.`

const continuePrompt = (s, check, round) => `${PROJECT}

Task: continuation ${round} of ${gddPath(s)} ("${s.name}", date ${DATE}). A previous author stopped before finishing. Keep all existing content; write ONLY these missing sections: ${JSON.stringify(check.missingSections)}${check.knobsUsedButNotListed && check.knobsUsedButNotListed.length ? `, and add these knobs to Tuning Knobs: ${JSON.stringify(check.knobsUsedButNotListed)}` : ''}.
Read the whole GDD once so the new sections match its rules, names and numbers; look up the GDDs it depends on per the token economy rules.
${READ_ECONOMY}
${sectionsSpec}
${decisionsBlock(s)}
${choiceRules}
${WRITE_STYLE}

${RULES}`

const reviewPrompt = (s, round) => round > 1 ? `${PROJECT}

Task: VERIFICATION review of ${gddPath(s)} ("${s.name}"), round ${round}, date ${DATE}, after a fix round. Do not redo the full review.
Read the previous report ${reviewPath(s)} and the GDD once. For every blocking finding of the previous report decide resolved / not resolved; then scan only the sections the fixes touched for new contradictions, bare numbers or unmeasurable criteria. Interfaces of other GDDs: Grep only what the touched sections reference.
${READ_ECONOMY}
Rewrite ${reviewPath(s)} as the round-${round} report (format ${SKILLS}/game-design-review/references/review-report.md): carried-over recommended findings stay listed. Do NOT edit the GDD, its Status line, systems-index.md or entities.yaml.
Return all findings still open (previous recommended ones included) plus new ones; needsUser=true only for decisions the user must take. Verdict APPROVED only if no blocking finding remains. At most 25 findings.

${RULES}` : `${PROJECT}

Task: independent design review of ${gddPath(s)} ("${s.name}"), round ${round}, date ${DATE}. You did not write this document and you get no design history: read everything from disk.
${REVIEW_SKILL}
${READ_ECONOMY}
Follow the game-design-review skill at depth lean (all phases, no specialist subagents) with these replacements:
- Skip the freshness shortcut in phase 1: always do a full review. If an earlier review report for this system exists in Docs/design/gdd/reviews/, read the latest one and track whether its blocking items were addressed.
- No user questions. Write the report (format: references/review-report.md) to ${reviewPath(s)}, overwriting any file of that name; state "Round ${round}" in it. Do NOT edit the GDD itself, its Status line, systems-index.md or entities.yaml.
- Check especially: required sections present and non-empty; formulas produce the described behaviour (plug in min/max inputs); every number is a Tuning Knob; acceptance criteria measurable; dependencies bidirectional against existing GDDs; event/interface names and payloads match other GDDs on disk; no contradiction with entities.yaml (except changes listed as user-approved below); pillars and tone from game-concept.md; Unity 6000.3 APIs the design relies on.
- User-approved changes that are not contradictions: ${JSON.stringify(REGISTRY_CHANGES)}.
- Open Questions tagged NEEDS-USER are decisions waiting for the user: report each as a finding with needsUser=true (severity blocking only if the document cannot be implemented without it).
- For each finding: severity blocking or recommended; needsUser=true ONLY when resolving it requires a major design decision the user must take; mechanical, wording, missing-detail and math fixes are needsUser=false. Give a concrete suggestedFix.
- Verdict: APPROVED only if there are no blocking findings. Keep the returned findings concise (at most 25, most severe first).

${RULES}`

const fixPrompt = (s, items, round) => `${PROJECT}

Task: fix round ${round} for ${gddPath(s)} ("${s.name}"), date ${DATE}. You are not the author and not the reviewer. Read the GDD once; look up only what the findings below need in the review report ${reviewPath(s)}, entities.yaml and related GDDs.
${READ_ECONOMY}
Fix ONLY these blocking findings, keeping all other content unchanged; follow the game-design skill's section formats:
${JSON.stringify(items, null, 2)}
${decisionsBlock(s)}
Keep the header Status line as "In Review". If a finding cannot be fixed without a major user decision, do not fix it: add an Open Question tagged NEEDS-USER and say so in your reply.
${WRITE_STYLE}

${RULES}`

const MAX_AGENTS = typeof A.maxAgents === 'number' ? A.maxAgents : 40
const RESERVED_TAIL = 2
let spawned = 0
const spawn = (prompt, opts) => { spawned++; return agent(prompt, opts) }
const canAfford = n => spawned + n <= MAX_AGENTS - RESERVED_TAIL
const run = async (stage, s, ctx, prompt, opts) => {
  const o = routed(stage, s, ctx, opts)
  let res = null
  let err = null
  try { res = await spawn(prompt, o) } catch (e) { err = e }
  if (res) return res
  if (o.model === TIER.high || !canAfford(1)) { if (err) throw err; return null }
  log(`${opts.label}: ${err ? 'failed' : 'empty result'} on ${o.model || 'inherit'}, retrying on ${TIER.high}`)
  modelUse[TIER.high] = (modelUse[TIER.high] || 0) + 1
  return spawn(prompt, Object.assign({}, opts, { model: TIER.high, label: `${opts.label} [retry ${TIER.high}]` }))
}

const gates = {}
for (const s of SYSTEMS) {
  let release
  const pr = new Promise(res => { release = res })
  gates[s.name] = { pr, release }
}

const runSystem = async s => {
  const kind = s.kind || 'new'
  try {
    const waitFor = (s.after || []).filter(n => gates[n])
    if (waitFor.length) {
      log(`${s.name}: waiting for ${waitFor.join(', ')}`)
      await Promise.all(waitFor.map(n => gates[n].pr))
    }
    const authorNotes = []
    if (kind !== 'review') {
      const note = await run('draft', s, null, authorPrompt(s), { label: `${kind}:${s.name}`, phase: 'Draft' })
      if (note) authorNotes.push(String(note).slice(0, 2000))
    }
    let check = await run('check', s, null, completenessPrompt(s), {
      label: `check:${s.name}#0`, phase: 'Complete', schema: COMPLETENESS_SCHEMA, effort: 'low',
    })
    for (let c = 1; check && !check.complete && c <= MAX_CONTINUE && canAfford(3) && !overBudget(); c++) {
      const note = await run('continue', s, null, continuePrompt(s, check, c), { label: `continue:${s.name}#${c}`, phase: 'Complete' })
      if (note) authorNotes.push(String(note).slice(0, 2000))
      check = await run('check', s, null, completenessPrompt(s), {
        label: `check:${s.name}#${c}`, phase: 'Complete', schema: COMPLETENESS_SCHEMA, effort: 'low',
      })
    }
    if (check && !check.complete) log(`${s.name}: still incomplete after ${MAX_CONTINUE} continuation rounds: ${check.missingSections.join(', ')}`)
    let review = null
    let fixRounds = 0
    let prevFixable = null
    for (let round = 1; ; round++) {
      review = await run('review', s, { round }, reviewPrompt(s, round), {
        label: `review:${s.name}#${round}`, phase: 'Review', schema: REVIEW_SCHEMA,
      })
      if (!review) break
      const fixable = review.findings.filter(f => f.severity === 'blocking' && !f.needsUser)
      if (!fixable.length || fixRounds >= MAX_FIX) break
      if (!canAfford(2)) { log(`${s.name}: agent budget ${MAX_AGENTS} reached, skipping fix round ${fixRounds + 1}`); break }
      if (overBudget()) { log(`${s.name}: output-token budget ${OUT_BUDGET} reached, skipping fix round ${fixRounds + 1}`); break }
      fixRounds++
      const escalate = prevFixable !== null && fixable.length >= prevFixable
      if (escalate) log(`${s.name}: blocking findings did not decrease (${prevFixable} → ${fixable.length}), escalating fix to ${TIER.high}`)
      prevFixable = fixable.length
      const note = await run('fix', s, { escalate }, fixPrompt(s, fixable, fixRounds), { label: `fix:${s.name}#${fixRounds}`, phase: 'Fix' })
      if (note) authorNotes.push(String(note).slice(0, 2000))
    }
    const findings = review ? review.findings : []
    const remainingBlocking = findings.filter(f => f.severity === 'blocking')
    return {
      name: s.name,
      path: gddPath(s),
      kind,
      complete: check ? check.complete : null,
      missingSections: check ? check.missingSections : [],
      verdict: review ? review.verdict : 'NOT ASSESSED',
      status: review && remainingBlocking.length === 0 && (!check || check.complete) ? 'Approved' : 'Needs Revision',
      reportPath: review ? review.reportPath : null,
      fixRounds,
      remainingBlocking,
      recommended: findings.filter(f => f.severity === 'recommended'),
      needsUser: findings.filter(f => f.needsUser),
      authorNotes,
    }
  } catch (e) {
    return { name: s.name, path: gddPath(s), kind, error: String(e) }
  } finally {
    gates[s.name].release()
  }
}

const results = await Promise.all(SYSTEMS.map(s => runSystem(s)))
log(`agents spawned for systems: ${spawned} (cap ${MAX_AGENTS}); models: ${JSON.stringify(modelUse)}; output tokens this turn: ${budget.spent()}${OUT_BUDGET !== null ? ` / ${OUT_BUDGET}` : ''}`)

phase('Consistency')
const batchFiles = results.map(r => r.path)
const consistency = await agent(
`${PROJECT}

Task: cross-check consistency of this batch of GDDs (date ${DATE}). READ ONLY: do not edit any file.
Batch: ${JSON.stringify(batchFiles)}
For each batch GDD read only its Summary, Detailed Design interaction table, Dependencies, Tuning Knobs and Cross-References (Grep headings, then Read those ranges). For other existing GDDs the batch references, plus systems-index.md and entities.yaml, Grep only the names being checked.\n${READ_ECONOMY}
User-approved registry changes (not contradictions): ${JSON.stringify(REGISTRY_CHANGES)}.
Check: dependencies are bidirectional and point the same direction in both GDDs and in systems-index.md; event names, payload fields and interface/API names match across GDDs; knob ownership is not duplicated (a value owned by one GDD is referenced, not redefined, by another); no value contradicts entities.yaml or another GDD; no contradiction with the pillars in game-concept.md.
Return findings with severity, affected systems and files, and a concrete suggested fix (at most 25, most severe first).

${RULES}`,
  routed('consistency', null, null, { label: 'consistency', phase: 'Consistency', schema: CONSISTENCY_SCHEMA }),
)

phase('Sync')
const syncInput = results.map(r => ({
  name: r.name, path: r.path, status: r.error ? 'Needs Revision' : r.status, verdict: r.verdict || null,
  reportPath: r.reportPath || null, error: r.error || null,
}))
const sync = await agent(
`${PROJECT}

Task: you are the ONLY writer of Docs/design/gdd/systems-index.md and Docs/design/registry/entities.yaml for this batch (date ${DATE}), and you also set each batch GDD's header "> **Status**:" line. Use the data below plus what you read from disk.
Batch results:
${JSON.stringify(syncInput, null, 2)}
Consistency findings (informational; do not fix GDD content):
${JSON.stringify(consistency ? consistency.findings : [], null, 2)}
User-approved registry changes to APPLY (these override the no-change rule):
${JSON.stringify(REGISTRY_CHANGES, null, 2)}
Do:
1. For each system: set the GDD header line to "> **Status**: <status>" and its systems-index row Status to the same word; set the row's Design Doc to its path; update "Last Updated" of the index to ${DATE}. Status words come from the data (Approved or Needs Revision); never invent another.
2. Make each batch system's systems-index row "Depends On" and its Dependency Map line match the Dependencies section of its GDD on disk (only edges where the GDD says this system depends on the other). Recompute bottlenecks and the Progress Tracker (GDDs started / reviewed / approved, MVP and Vertical Slice designed counts) from the rows.
3. entities.yaml: apply the user-approved changes above. Then scan the batch GDDs for named values and formulas that another GDD references (constants, formulas, entities such as pets): add missing ones under the right list with source = the owning GDD, and fill referenced_by with every GDD that references an entry. If a GDD gives a different value for an existing name that is not in the approved changes, DO NOT change it: list it in registryConflicts. Keep the file valid YAML.
Edit only those files (systems-index.md, entities.yaml, and the Status line of each batch GDD).

${RULES.replace('- Never edit Docs/design/gdd/systems-index.md, Docs/design/registry/entities.yaml or Docs/production/session-state/active.md: a separate Sync step is their only writer. Report what they need instead.\n', '')}`,
  routed('sync', null, null, { label: 'sync', phase: 'Sync', schema: SYNC_SCHEMA }),
)

return {
  mode: 'design',
  date: DATE,
  systems: results.map(r => ({
    name: r.name,
    path: r.path,
    kind: r.kind,
    error: r.error || null,
    complete: r.complete === undefined ? null : r.complete,
    missingSections: r.missingSections || [],
    verdict: r.verdict || null,
    status: r.error ? 'Needs Revision' : r.status,
    fixRounds: r.fixRounds || 0,
    reportPath: r.reportPath || null,
    remainingBlocking: r.remainingBlocking || [],
    needsUser: r.needsUser || [],
    recommended: r.recommended || [],
    authorNotes: r.authorNotes || [],
  })),
  consistency: consistency ? consistency.findings : null,
  sync,
  modelUse,
}
