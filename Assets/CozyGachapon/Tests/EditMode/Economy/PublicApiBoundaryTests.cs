using System;
using System.Collections.Generic;
using System.Reflection;
using CozyGachapon.Contracts;
using CozyGachapon.Economy;
using NUnit.Framework;

namespace CozyGachapon.Tests.Economy
{
    [TestFixture]
    public sealed class PublicApiBoundaryTests
    {
        private const double InitialCoinBalance = 500d;
        private const double InitialStampBalance = 7d;

        [Test]
        public void AC7b_InvokeAllNonSpendPublicMethodsWithBoundaryInputs_BalancesNeverDecreaseAndNoExceptionsEscape()
        {
            EconomyConfig config = EconomyTestAssets.LoadConfig();
            CountingEconomyDiagnosticsSink sink = new CountingEconomyDiagnosticsSink();
            FakeSimulationState simState = new FakeSimulationState(true, false);
            RecordingWalletEvents events = new RecordingWalletEvents();

            EconomyStateData loaded = new EconomyStateData(
                new[]
                {
                    new CurrencyBalanceEntry(EconomyIds.Coin, InitialCoinBalance),
                    new CurrencyBalanceEntry(EconomyIds.ShopStamp, InitialStampBalance)
                },
                0);

            EconomyCore core = new EconomyCore(sink, simState);
            Assert.That(core.Initialize(config, ReleaseTier.Alpha, loaded, simState), Is.True);
            core.WalletChanged += events.OnWalletChanged;

            InvokeAllNonActionMethods(core, core.GetType(), core, events, simState);
            InvokeAllNonActionMethods(core.IncomePipeline, typeof(IIncomePipeline), core, events, simState);
        }

        private static void InvokeAllNonActionMethods(
            object targetInstance,
            Type targetType,
            EconomyCore core,
            RecordingWalletEvents events,
            FakeSimulationState simState)
        {
            MethodInfo[] methods = targetType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

            for (int m = 0; m < methods.Length; m++)
            {
                MethodInfo method = methods[m];
                if (ShouldSkipMethod(method))
                {
                    continue;
                }

                List<object[]> combinations = EconomyApiInvoker.BuildParameterCombinations(method);
                for (int c = 0; c < combinations.Count; c++)
                {
                    object[] args = combinations[c];
                    Exception escaped = null;

                    try
                    {
                        method.Invoke(targetInstance, args);
                    }
                    catch (TargetInvocationException tie)
                    {
                        escaped = tie.InnerException ?? tie;
                    }
                    catch (Exception ex)
                    {
                        escaped = ex;
                    }

                    Assert.That(escaped, Is.Null, "Method " + targetType.Name + "." + method.Name + " threw: " + escaped);
                    Assert.That(
                        core.GetBalance(EconomyIds.Coin),
                        Is.GreaterThanOrEqualTo(InitialCoinBalance),
                        "Coin balance dropped below 500 after " + method.Name);
                    Assert.That(
                        core.GetBalance(EconomyIds.ShopStamp),
                        Is.GreaterThanOrEqualTo(InitialStampBalance),
                        "ShopStamp balance dropped below 7 after " + method.Name);

                    for (int e = 0; e < events.WalletChanges.Count; e++)
                    {
                        Assert.That(
                            events.WalletChanges[e].Delta,
                            Is.GreaterThanOrEqualTo(0d),
                            "Negative WalletChanged delta after " + method.Name);
                    }

                    core.SetSimulationState(simState);
                }
            }
        }

        private static bool ShouldSkipMethod(MethodInfo method)
        {
            if (method.IsSpecialName)
            {
                return true;
            }

            if (string.Equals(method.Name, "Initialize", StringComparison.Ordinal))
            {
                return true;
            }

            ParameterInfo[] parameters = method.GetParameters();
            for (int i = 0; i < parameters.Length; i++)
            {
                Type paramType = parameters[i].ParameterType;
                if (paramType.IsByRef)
                {
                    paramType = paramType.GetElementType();
                }

                if (paramType == ActionContextReflectionTests.ActionContextType)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
