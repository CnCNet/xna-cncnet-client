using System;

namespace ClientCore.Extensions;

// https://stackoverflow.com/a/65894979/20766970
public static class ArrayExtensions
{
    public static void Deconstruct<T>(this T[] @this, out T a0)
    {
        if (@this == null || @this.Length < 1)
            throw new ArgumentException(null, nameof(@this));

        a0 = @this[0];
    }

    public static (T, T) AsTuple2<T>(this T[] @this)
    {
        if (@this == null || @this.Length < 2)
            throw new ArgumentException(null, nameof(@this));

        return (@this[0], @this[1]);
    }

    public static void Deconstruct<T>(this T[] @this, out T a0, out T a1)
        => (a0, a1) = @this.AsTuple2();

    public static (T, T, T) AsTuple3<T>(this T[] @this)
    {
        if (@this == null || @this.Length < 3)
            throw new ArgumentException(null, nameof(@this));

        return (@this[0], @this[1], @this[2]);
    }

    public static void Deconstruct<T>(this T[] @this, out T a0, out T a1, out T a2)
        => (a0, a1, a2) = @this.AsTuple3();

    public static (T, T, T, T) AsTuple4<T>(this T[] @this)
    {
        if (@this == null || @this.Length < 4)
            throw new ArgumentException(null, nameof(@this));

        return (@this[0], @this[1], @this[2], @this[3]);
    }

    public static void Deconstruct<T>(this T[] @this, out T a0, out T a1, out T a2, out T a3)
        => (a0, a1, a2, a3) = @this.AsTuple4();

    public static (T, T, T, T, T) AsTuple5<T>(this T[] @this)
    {
        if (@this == null || @this.Length < 5)
            throw new ArgumentException(null, nameof(@this));

        return (@this[0], @this[1], @this[2], @this[3], @this[4]);
    }

    public static void Deconstruct<T>(this T[] @this, out T a0, out T a1, out T a2, out T a3, out T a4)
        => (a0, a1, a2, a3, a4) = @this.AsTuple5();

    public static (T, T, T, T, T, T) AsTuple6<T>(this T[] @this)
    {
        if (@this == null || @this.Length < 6)
            throw new ArgumentException(null, nameof(@this));

        return (@this[0], @this[1], @this[2], @this[3], @this[4], @this[5]);
    }

    public static void Deconstruct<T>(this T[] @this, out T a0, out T a1, out T a2, out T a3, out T a4, out T a5)
        => (a0, a1, a2, a3, a4, a5) = @this.AsTuple6();

    public static (T, T, T, T, T, T, T) AsTuple7<T>(this T[] @this)
    {
        if (@this == null || @this.Length < 7)
            throw new ArgumentException(null, nameof(@this));

        return (@this[0], @this[1], @this[2], @this[3], @this[4], @this[5], @this[6]);
    }

    public static void Deconstruct<T>(this T[] @this, out T a0, out T a1, out T a2, out T a3, out T a4, out T a5, out T a6)
        => (a0, a1, a2, a3, a4, a5, a6) = @this.AsTuple7();

    public static (T, T, T, T, T, T, T, T) AsTuple8<T>(this T[] @this)
    {
        if (@this == null || @this.Length < 8)
            throw new ArgumentException(null, nameof(@this));

        return (@this[0], @this[1], @this[2], @this[3], @this[4], @this[5], @this[6], @this[7]);
    }

    public static void Deconstruct<T>(this T[] @this, out T a0, out T a1, out T a2, out T a3, out T a4, out T a5, out T a6, out T a7)
        => (a0, a1, a2, a3, a4, a5, a6, a7) = @this.AsTuple8();
}
