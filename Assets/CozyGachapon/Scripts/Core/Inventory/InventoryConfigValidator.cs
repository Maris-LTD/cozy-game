using System;
using System.Collections.Generic;
using CozyGachapon.Core.PetCatalog;

namespace CozyGachapon.Core.Inventory
{
    public sealed class InventoryValidationReport
    {
        public List<string> Errors { get; } = new List<string>();
        public List<string> Warnings { get; } = new List<string>();
        public bool IsValid => Errors.Count == 0;
    }

    public static class InventoryConfigValidator
    {
        public const int MinCopiesDisplayMax = 9;
        public const int MaxCopiesDisplayMax = 999;
        public const int MinMaxQueuedEvents = 8;
        public const int MaxMaxQueuedEvents = 1024;

        public static InventoryValidationReport Validate(InventoryConfig config, IPetDatabase catalog)
        {
            var report = new InventoryValidationReport();
            if (config == null)
            {
                report.Errors.Add("InventoryConfig is null.");
                return report;
            }

            if (config.CopiesDisplayMax < MinCopiesDisplayMax || config.CopiesDisplayMax > MaxCopiesDisplayMax)
            {
                report.Errors.Add($"copiesDisplayMax ({config.CopiesDisplayMax}) must be between {MinCopiesDisplayMax} and {MaxCopiesDisplayMax}.");
            }

            if (config.MaxQueuedEvents < MinMaxQueuedEvents || config.MaxQueuedEvents > MaxMaxQueuedEvents)
            {
                report.Errors.Add($"maxQueuedEvents ({config.MaxQueuedEvents}) must be between {MinMaxQueuedEvents} and {MaxMaxQueuedEvents}.");
            }

            var starting = config.StartingPets;
            if (starting == null || starting.Count == 0)
            {
                return report;
            }

            if (starting.Count > 1)
            {
                report.Warnings.Add($"startingPets contains {starting.Count} entries (> 1 entry warning at launch).");
            }

            var seenIds = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < starting.Count; i++)
            {
                var entry = starting[i];
                if (string.IsNullOrEmpty(entry.Id) || catalog == null || !catalog.TryGet(entry.Id, out var def) || def == null)
                {
                    report.Errors.Add($"startingPets[{i}] has unknown pet ID '{entry.Id}'.");
                }
                else
                {
                    if (!seenIds.Add(entry.Id))
                    {
                        report.Errors.Add($"startingPets[{i}] has duplicate pet ID '{entry.Id}'.");
                    }
                    if (def.IsDeprecated)
                    {
                        report.Warnings.Add($"startingPets[{i}] references deprecated pet '{entry.Id}'.");
                    }
                }

                if (entry.Copies < 1)
                {
                    report.Errors.Add($"startingPets[{i}] ('{entry.Id}') has invalid copies ({entry.Copies} < 1).");
                }
            }

            return report;
        }
    }
}
