using System;
using Unity.Profiling;

namespace CozyGachapon.Tests.Pooling.StaticFakes
{
    public enum FakeMode
    {
        None = 0,
        Some = 1
    }

    public static class MutableStaticFieldFake
    {
        public static int Counter;
    }

    public static class StaticAutoPropertyFake
    {
        public static int Total { get; set; }
    }

    public static class StaticEventFake
    {
        public static event Action Fired;

        public static void Raise()
        {
            Fired?.Invoke();
        }
    }

    public static class StaticReadonlyReferenceFake
    {
        public static readonly int[] Table = new int[4];
    }

    public static class ImmutableStaticFake
    {
        public const int ConstantInt = 3;
        public const string ConstantText = "fake";
        public static readonly string ReadonlyText = "fake";
        public static readonly bool ReadonlyFlag = true;
        public static readonly char ReadonlyChar = 'x';
        public static readonly long ReadonlyLong = 7L;
        public static readonly FakeMode ReadonlyMode = FakeMode.Some;
        public static readonly ProfilerMarker Marker = new ProfilerMarker("fake");
    }

    public static class LambdaCacheFake
    {
        public static Func<int> Make()
        {
            return () => 1;
        }

        public static Func<int> MakeCapturing(int value)
        {
            return () => value;
        }
    }

    public sealed class GenericMutableFake<T>
    {
        public static T Cached;
    }

    public sealed class NestedMutableFake
    {
        public static class Inner
        {
            public static int Value;
        }
    }
}
