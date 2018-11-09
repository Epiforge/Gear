using Gear.Components;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Gear.ActiveExpressions
{
    public static class ExpressionOperations
    {
        static readonly IReadOnlyList<(MethodInfo methodInfo, ExpressionOperationAttribute expressionOperationAttribute)> operationMethods = typeof(ExpressionOperations).GetRuntimeMethods().Where(m => m.IsPublic && m.IsStatic).Select(m => (methodInfo: m, expressionOperationAttribute: m.GetCustomAttribute<ExpressionOperationAttribute>())).Where(m => m.expressionOperationAttribute != null).ToImmutableArray();
        static ConcurrentDictionary<(ExpressionType expressionType, Type returnType, EquatableList<Type> parameterTypes), FastMethodInfo> operationFastMethodInfos = new ConcurrentDictionary<(ExpressionType expressionType, Type returnType, EquatableList<Type> parameterTypes), FastMethodInfo>();

        static FastMethodInfo CreateFastMethodInfo((ExpressionType expressionType, Type returnType, EquatableList<Type> parameterTypes) key)
        {
            var (expressionType, returnType, parameterTypes) = key;
            var (methodInfo, expressionOperationAttribute) = operationMethods.SingleOrDefault(m =>
            {
                var (mi, eoa) = m;
                return eoa.Type == expressionType && mi.ReturnType == returnType && mi.GetParameters().Select(p => p.ParameterType).SequenceEqual(parameterTypes);
            });
            if (methodInfo == default)
                return null;
            return new FastMethodInfo(methodInfo);
        }

        public static FastMethodInfo GetFastMethodInfo(ExpressionType expressionType, Type returnType, params Type[] parameterTypes) => operationFastMethodInfos.GetOrAdd((expressionType, returnType, new EquatableList<Type>(parameterTypes)), CreateFastMethodInfo);

        #region Add

        [ExpressionOperation(ExpressionType.Add)]
        public static byte Add(byte a, byte b) => (byte)(a + b);

        [ExpressionOperation(ExpressionType.Add)]
        public static byte? Add(byte? a, byte? b) => (byte?)(a + b);

        [ExpressionOperation(ExpressionType.Add)]
        public static sbyte Add(sbyte a, sbyte b) => (sbyte)(a + b);

        [ExpressionOperation(ExpressionType.Add)]
        public static sbyte? Add(sbyte? a, sbyte? b) => (sbyte?)(a + b);

        [ExpressionOperation(ExpressionType.Add)]
        public static short Add(short a, short b) => (short)(a + b);

        [ExpressionOperation(ExpressionType.Add)]
        public static short? Add(short? a, short? b) => (short?)(a + b);

        [ExpressionOperation(ExpressionType.Add)]
        public static ushort Add(ushort a, ushort b) => (ushort)(a + b);

        [ExpressionOperation(ExpressionType.Add)]
        public static ushort? Add(ushort? a, ushort? b) => (ushort?)(a + b);

        [ExpressionOperation(ExpressionType.Add)]
        public static int Add(int a, int b) => a + b;

        [ExpressionOperation(ExpressionType.Add)]
        public static int? Add(int? a, int? b) => a + b;

        [ExpressionOperation(ExpressionType.Add)]
        public static uint Add(uint a, uint b) => a + b;

        [ExpressionOperation(ExpressionType.Add)]
        public static uint? Add(uint? a, uint? b) => a + b;

        [ExpressionOperation(ExpressionType.Add)]
        public static long Add(long a, long b) => a + b;

        [ExpressionOperation(ExpressionType.Add)]
        public static long? Add(long? a, long? b) => a + b;

        [ExpressionOperation(ExpressionType.Add)]
        public static ulong Add(ulong a, ulong b) => a + b;

        [ExpressionOperation(ExpressionType.Add)]
        public static ulong? Add(ulong? a, ulong? b) => a + b;

        [ExpressionOperation(ExpressionType.Add)]
        public static float Add(float a, float b) => a + b;

        [ExpressionOperation(ExpressionType.Add)]
        public static float? Add(float? a, float? b) => a + b;

        [ExpressionOperation(ExpressionType.Add)]
        public static double Add(double a, double b) => a + b;

        [ExpressionOperation(ExpressionType.Add)]
        public static double? Add(double? a, double? b) => a + b;

        [ExpressionOperation(ExpressionType.Add)]
        public static decimal Add(decimal a, decimal b) => a + b;

        [ExpressionOperation(ExpressionType.Add)]
        public static decimal? Add(decimal? a, decimal? b) => a + b;

        [ExpressionOperation(ExpressionType.Add)]
        public static DateTime Add(DateTime a, TimeSpan b) => a + b;

        [ExpressionOperation(ExpressionType.Add)]
        public static DateTime? Add(DateTime? a, TimeSpan? b) => a + b;

        [ExpressionOperation(ExpressionType.Add)]
        public static TimeSpan Add(TimeSpan a, TimeSpan b) => a + b;

        [ExpressionOperation(ExpressionType.Add)]
        public static TimeSpan? Add(TimeSpan? a, TimeSpan? b) => a + b;

        [ExpressionOperation(ExpressionType.Add)]
        public static string Add(string a, string b) => a + b;

        #endregion Add

        #region AddChecked

        [ExpressionOperation(ExpressionType.AddChecked)]
        public static byte AddChecked(byte a, byte b) => checked((byte)(a + b));

        [ExpressionOperation(ExpressionType.AddChecked)]
        public static byte? AddChecked(byte? a, byte? b) => checked((byte?)(a + b));

        [ExpressionOperation(ExpressionType.AddChecked)]
        public static sbyte AddChecked(sbyte a, sbyte b) => checked((sbyte)(a + b));

        [ExpressionOperation(ExpressionType.AddChecked)]
        public static sbyte? AddChecked(sbyte? a, sbyte? b) => checked((sbyte?)(a + b));

        [ExpressionOperation(ExpressionType.AddChecked)]
        public static short AddChecked(short a, short b) => checked((short)(a + b));

        [ExpressionOperation(ExpressionType.AddChecked)]
        public static short? AddChecked(short? a, short? b) => checked((short?)(a + b));

        [ExpressionOperation(ExpressionType.AddChecked)]
        public static ushort AddChecked(ushort a, ushort b) => checked((ushort)(a + b));

        [ExpressionOperation(ExpressionType.AddChecked)]
        public static ushort? AddChecked(ushort? a, ushort? b) => checked((ushort?)(a + b));

        [ExpressionOperation(ExpressionType.AddChecked)]
        public static int AddChecked(int a, int b) => checked(a + b);

        [ExpressionOperation(ExpressionType.AddChecked)]
        public static int? AddChecked(int? a, int? b) => checked(a + b);

        [ExpressionOperation(ExpressionType.AddChecked)]
        public static uint AddChecked(uint a, uint b) => checked(a + b);

        [ExpressionOperation(ExpressionType.AddChecked)]
        public static uint? AddChecked(uint? a, uint? b) => checked(a + b);

        [ExpressionOperation(ExpressionType.AddChecked)]
        public static long AddChecked(long a, long b) => checked(a + b);

        [ExpressionOperation(ExpressionType.AddChecked)]
        public static long? AddChecked(long? a, long? b) => checked(a + b);

        [ExpressionOperation(ExpressionType.AddChecked)]
        public static ulong AddChecked(ulong a, ulong b) => checked(a + b);

        [ExpressionOperation(ExpressionType.AddChecked)]
        public static ulong? AddChecked(ulong? a, ulong? b) => checked(a + b);

        #endregion AddChecked

        #region And

        [ExpressionOperation(ExpressionType.And)]
        public static bool And(bool a, bool b) => a & b;

        [ExpressionOperation(ExpressionType.And)]
        public static byte And(byte a, byte b) => (byte)(a & b);

        [ExpressionOperation(ExpressionType.And)]
        public static byte? And(byte? a, byte? b) => (byte?)(a & b);

        [ExpressionOperation(ExpressionType.And)]
        public static sbyte And(sbyte a, sbyte b) => (sbyte)(a & b);

        [ExpressionOperation(ExpressionType.And)]
        public static sbyte? And(sbyte? a, sbyte? b) => (sbyte?)(a & b);

        [ExpressionOperation(ExpressionType.And)]
        public static short And(short a, short b) => (short)(a & b);

        [ExpressionOperation(ExpressionType.And)]
        public static short? And(short? a, short? b) => (short?)(a & b);

        [ExpressionOperation(ExpressionType.And)]
        public static ushort And(ushort a, ushort b) => (ushort)(a & b);

        [ExpressionOperation(ExpressionType.And)]
        public static ushort? And(ushort? a, ushort? b) => (ushort?)(a & b);

        [ExpressionOperation(ExpressionType.And)]
        public static int And(int a, int b) => a & b;

        [ExpressionOperation(ExpressionType.And)]
        public static int? And(int? a, int? b) => a & b;

        [ExpressionOperation(ExpressionType.And)]
        public static uint And(uint a, uint b) => a & b;

        [ExpressionOperation(ExpressionType.And)]
        public static uint? And(uint? a, uint? b) => a & b;

        [ExpressionOperation(ExpressionType.And)]
        public static long And(long a, long b) => a & b;

        [ExpressionOperation(ExpressionType.And)]
        public static long? And(long? a, long? b) => a & b;

        [ExpressionOperation(ExpressionType.And)]
        public static ulong And(ulong a, ulong b) => a & b;

        [ExpressionOperation(ExpressionType.And)]
        public static ulong? And(ulong? a, ulong? b) => a & b;

        #endregion And

        #region Convert

        #region ToBoolean

        [ExpressionOperation(ExpressionType.Convert)]
        public static bool ConvertToBoolean(bool a) => a;

        [ExpressionOperation(ExpressionType.Convert)]
        public static bool? ConvertToBoolean(bool? a) => a;

        [ExpressionOperation(ExpressionType.Convert)]
        public static bool ConvertToBoolean(byte a) => Convert.ToBoolean(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static bool? ConvertToBoolean(byte? a) => a == null ? (bool?)null : Convert.ToBoolean(a.Value);

        [ExpressionOperation(ExpressionType.Convert)]
        public static bool ConvertToBoolean(sbyte a) => Convert.ToBoolean(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static bool? ConvertToBoolean(sbyte? a) => a == null ? (bool?)null : Convert.ToBoolean(a.Value);

        [ExpressionOperation(ExpressionType.Convert)]
        public static bool ConvertToBoolean(short a) => Convert.ToBoolean(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static bool? ConvertToBoolean(short? a) => a == null ? (bool?)null : Convert.ToBoolean(a.Value);

        [ExpressionOperation(ExpressionType.Convert)]
        public static bool ConvertToBoolean(ushort a) => Convert.ToBoolean(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static bool? ConvertToBoolean(ushort? a) => a == null ? (bool?)null : Convert.ToBoolean(a.Value);

        [ExpressionOperation(ExpressionType.Convert)]
        public static bool ConvertToBoolean(int a) => Convert.ToBoolean(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static bool? ConvertToBoolean(int? a) => a == null ? (bool?)null : Convert.ToBoolean(a.Value);

        [ExpressionOperation(ExpressionType.Convert)]
        public static bool ConvertToBoolean(uint a) => Convert.ToBoolean(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static bool? ConvertToBoolean(uint? a) => a == null ? (bool?)null : Convert.ToBoolean(a.Value);

        [ExpressionOperation(ExpressionType.Convert)]
        public static bool ConvertToBoolean(long a) => Convert.ToBoolean(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static bool? ConvertToBoolean(long? a) => a == null ? (bool?)null : Convert.ToBoolean(a.Value);

        [ExpressionOperation(ExpressionType.Convert)]
        public static bool ConvertToBoolean(ulong a) => Convert.ToBoolean(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static bool? ConvertToBoolean(ulong? a) => a == null ? (bool?)null : Convert.ToBoolean(a.Value);

        [ExpressionOperation(ExpressionType.Convert)]
        public static bool ConvertToBoolean(float a) => Convert.ToBoolean(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static bool? ConvertToBoolean(float? a) => a == null ? (bool?)null : Convert.ToBoolean(a.Value);

        [ExpressionOperation(ExpressionType.Convert)]
        public static bool ConvertToBoolean(double a) => Convert.ToBoolean(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static bool? ConvertToBoolean(double? a) => a == null ? (bool?)null : Convert.ToBoolean(a.Value);

        [ExpressionOperation(ExpressionType.Convert)]
        public static bool ConvertToBoolean(decimal a) => Convert.ToBoolean(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static bool? ConvertToBoolean(decimal? a) => a == null ? (bool?)null : Convert.ToBoolean(a.Value);

        [ExpressionOperation(ExpressionType.Convert)]
        public static bool ConvertToBoolean(string a) => Convert.ToBoolean(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static bool ConvertToBoolean(object a) => Convert.ToBoolean(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static bool? ConvertToNullableBoolean(object a) => a == null ? (bool?)null : Convert.ToBoolean(a);

        #endregion ToBoolean

        #region ToByte

        [ExpressionOperation(ExpressionType.Convert)]
        public static byte ConvertToByte(bool a) => Convert.ToByte(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static byte? ConvertToByte(bool? a) => a == null ? (byte?)null : Convert.ToByte(a.Value);

        [ExpressionOperation(ExpressionType.Convert)]
        public static byte ConvertToByte(byte a) => a;

        [ExpressionOperation(ExpressionType.Convert)]
        public static byte? ConvertToByte(byte? a) => a;

        [ExpressionOperation(ExpressionType.Convert)]
        public static byte ConvertToByte(sbyte a) => Convert.ToByte(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static byte? ConvertToByte(sbyte? a) => a == null ? (byte?)null : Convert.ToByte(a.Value);

        [ExpressionOperation(ExpressionType.Convert)]
        public static byte ConvertToByte(short a) => Convert.ToByte(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static byte? ConvertToByte(short? a) => a == null ? (byte?)null : Convert.ToByte(a.Value);

        [ExpressionOperation(ExpressionType.Convert)]
        public static byte ConvertToByte(ushort a) => Convert.ToByte(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static byte? ConvertToByte(ushort? a) => a == null ? (byte?)null : Convert.ToByte(a.Value);

        [ExpressionOperation(ExpressionType.Convert)]
        public static byte ConvertToByte(int a) => Convert.ToByte(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static byte? ConvertToByte(int? a) => a == null ? (byte?)null : Convert.ToByte(a.Value);

        [ExpressionOperation(ExpressionType.Convert)]
        public static byte ConvertToByte(uint a) => Convert.ToByte(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static byte? ConvertToByte(uint? a) => a == null ? (byte?)null : Convert.ToByte(a.Value);

        [ExpressionOperation(ExpressionType.Convert)]
        public static byte ConvertToByte(long a) => Convert.ToByte(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static byte? ConvertToByte(long? a) => a == null ? (byte?)null : Convert.ToByte(a.Value);

        [ExpressionOperation(ExpressionType.Convert)]
        public static byte ConvertToByte(ulong a) => Convert.ToByte(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static byte? ConvertToByte(ulong? a) => a == null ? (byte?)null : Convert.ToByte(a.Value);

        [ExpressionOperation(ExpressionType.Convert)]
        public static byte ConvertToByte(float a) => Convert.ToByte(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static byte? ConvertToByte(float? a) => a == null ? (byte?)null : Convert.ToByte(a.Value);

        [ExpressionOperation(ExpressionType.Convert)]
        public static byte ConvertToByte(double a) => Convert.ToByte(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static byte? ConvertToByte(double? a) => a == null ? (byte?)null : Convert.ToByte(a.Value);

        [ExpressionOperation(ExpressionType.Convert)]
        public static byte ConvertToByte(decimal a) => Convert.ToByte(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static byte? ConvertToByte(decimal? a) => a == null ? (byte?)null : Convert.ToByte(a.Value);

        [ExpressionOperation(ExpressionType.Convert)]
        public static byte ConvertToByte(string a) => Convert.ToByte(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static byte ConvertToByte(object a) => Convert.ToByte(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static byte? ConvertToNullableByte(object a) => a == null ? (byte?)null : Convert.ToByte(a);

        #endregion ToByte

        #region ToSByte

        [ExpressionOperation(ExpressionType.Convert)]
        public static sbyte ConvertToSByte(bool a) => Convert.ToSByte(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static sbyte? ConvertToSByte(bool? a) => a == null ? (sbyte?)null : Convert.ToSByte(a.Value);

        [ExpressionOperation(ExpressionType.Convert)]
        public static sbyte ConvertToSByte(byte a) => Convert.ToSByte(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static sbyte? ConvertToSByte(byte? a) => a == null ? (sbyte?)null : Convert.ToSByte(a.Value);

        [ExpressionOperation(ExpressionType.Convert)]
        public static sbyte ConvertToSByte(sbyte a) => a;

        [ExpressionOperation(ExpressionType.Convert)]
        public static sbyte? ConvertToSByte(sbyte? a) => a;

        [ExpressionOperation(ExpressionType.Convert)]
        public static sbyte ConvertToSByte(short a) => Convert.ToSByte(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static sbyte? ConvertToSByte(short? a) => a == null ? (sbyte?)null : Convert.ToSByte(a.Value);

        [ExpressionOperation(ExpressionType.Convert)]
        public static sbyte ConvertToSByte(ushort a) => Convert.ToSByte(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static sbyte? ConvertToSByte(ushort? a) => a == null ? (sbyte?)null : Convert.ToSByte(a.Value);

        [ExpressionOperation(ExpressionType.Convert)]
        public static sbyte ConvertToSByte(int a) => Convert.ToSByte(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static sbyte? ConvertToSByte(int? a) => a == null ? (sbyte?)null : Convert.ToSByte(a.Value);

        [ExpressionOperation(ExpressionType.Convert)]
        public static sbyte ConvertToSByte(uint a) => Convert.ToSByte(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static sbyte? ConvertToSByte(uint? a) => a == null ? (sbyte?)null : Convert.ToSByte(a.Value);

        [ExpressionOperation(ExpressionType.Convert)]
        public static sbyte ConvertToSByte(long a) => Convert.ToSByte(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static sbyte? ConvertToSByte(long? a) => a == null ? (sbyte?)null : Convert.ToSByte(a.Value);

        [ExpressionOperation(ExpressionType.Convert)]
        public static sbyte ConvertToSByte(ulong a) => Convert.ToSByte(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static sbyte? ConvertToSByte(ulong? a) => a == null ? (sbyte?)null : Convert.ToSByte(a.Value);

        [ExpressionOperation(ExpressionType.Convert)]
        public static sbyte ConvertToSByte(float a) => Convert.ToSByte(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static sbyte? ConvertToSByte(float? a) => a == null ? (sbyte?)null : Convert.ToSByte(a.Value);

        [ExpressionOperation(ExpressionType.Convert)]
        public static sbyte ConvertToSByte(double a) => Convert.ToSByte(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static sbyte? ConvertToSByte(double? a) => a == null ? (sbyte?)null : Convert.ToSByte(a.Value);

        [ExpressionOperation(ExpressionType.Convert)]
        public static sbyte ConvertToSByte(decimal a) => Convert.ToSByte(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static sbyte? ConvertToSByte(decimal? a) => a == null ? (sbyte?)null : Convert.ToSByte(a.Value);

        [ExpressionOperation(ExpressionType.Convert)]
        public static sbyte ConvertToSByte(string a) => Convert.ToSByte(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static sbyte ConvertToSByte(object a) => Convert.ToSByte(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static sbyte? ConvertToNullableSByte(object a) => a == null ? (sbyte?)null : Convert.ToSByte(a);

        #endregion ToSByte

        #region ToInt16

        [ExpressionOperation(ExpressionType.Convert)]
        public static short ConvertToInt16(bool a) => Convert.ToInt16(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static short? ConvertToInt16(bool? a) => a == null ? (short?)null : Convert.ToInt16(a.Value);

        [ExpressionOperation(ExpressionType.Convert)]
        public static short ConvertToInt16(byte a) => Convert.ToInt16(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static short? ConvertToInt16(byte? a) => a == null ? (short?)null : Convert.ToInt16(a.Value);

        [ExpressionOperation(ExpressionType.Convert)]
        public static short ConvertToInt16(sbyte a) => Convert.ToInt16(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static short? ConvertToInt16(sbyte? a) => a == null ? (short?)null : Convert.ToInt16(a.Value);

        [ExpressionOperation(ExpressionType.Convert)]
        public static short ConvertToInt16(short a) => a;

        [ExpressionOperation(ExpressionType.Convert)]
        public static short? ConvertToInt16(short? a) => a;

        [ExpressionOperation(ExpressionType.Convert)]
        public static short ConvertToInt16(ushort a) => Convert.ToInt16(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static short? ConvertToInt16(ushort? a) => a == null ? (short?)null : Convert.ToInt16(a.Value);

        [ExpressionOperation(ExpressionType.Convert)]
        public static short ConvertToInt16(int a) => Convert.ToInt16(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static short? ConvertToInt16(int? a) => a == null ? (short?)null : Convert.ToInt16(a.Value);

        [ExpressionOperation(ExpressionType.Convert)]
        public static short ConvertToInt16(uint a) => Convert.ToInt16(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static short? ConvertToInt16(uint? a) => a == null ? (short?)null : Convert.ToInt16(a.Value);

        [ExpressionOperation(ExpressionType.Convert)]
        public static short ConvertToInt16(long a) => Convert.ToInt16(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static short? ConvertToInt16(long? a) => a == null ? (short?)null : Convert.ToInt16(a.Value);

        [ExpressionOperation(ExpressionType.Convert)]
        public static short ConvertToInt16(ulong a) => Convert.ToInt16(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static short? ConvertToInt16(ulong? a) => a == null ? (short?)null : Convert.ToInt16(a.Value);

        [ExpressionOperation(ExpressionType.Convert)]
        public static short ConvertToInt16(float a) => Convert.ToInt16(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static short? ConvertToInt16(float? a) => a == null ? (short?)null : Convert.ToInt16(a.Value);

        [ExpressionOperation(ExpressionType.Convert)]
        public static short ConvertToInt16(double a) => Convert.ToInt16(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static short? ConvertToInt16(double? a) => a == null ? (short?)null : Convert.ToInt16(a.Value);

        [ExpressionOperation(ExpressionType.Convert)]
        public static short ConvertToInt16(decimal a) => Convert.ToInt16(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static short? ConvertToInt16(decimal? a) => a == null ? (short?)null : Convert.ToInt16(a.Value);

        [ExpressionOperation(ExpressionType.Convert)]
        public static short ConvertToInt16(string a) => Convert.ToInt16(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static short ConvertToInt16(object a) => Convert.ToInt16(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static short? ConvertToNullableInt16(object a) => a == null ? (short?)null : Convert.ToInt16(a);

        #endregion ToInt16

        #region ToUInt16

        [ExpressionOperation(ExpressionType.Convert)]
        public static ushort ConvertToUInt16(bool a) => Convert.ToUInt16(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static ushort? ConvertToUInt16(bool? a) => a == null ? (ushort?)null : Convert.ToUInt16(a.Value);

        [ExpressionOperation(ExpressionType.Convert)]
        public static ushort ConvertToUInt16(byte a) => Convert.ToUInt16(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static ushort? ConvertToUInt16(byte? a) => a == null ? (ushort?)null : Convert.ToUInt16(a.Value);

        [ExpressionOperation(ExpressionType.Convert)]
        public static ushort ConvertToUInt16(sbyte a) => Convert.ToUInt16(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static ushort? ConvertToUInt16(sbyte? a) => a == null ? (ushort?)null : Convert.ToUInt16(a.Value);

        [ExpressionOperation(ExpressionType.Convert)]
        public static ushort ConvertToUInt16(short a) => Convert.ToUInt16(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static ushort? ConvertToUInt16(short? a) => a == null ? (ushort?)null : Convert.ToUInt16(a.Value);

        [ExpressionOperation(ExpressionType.Convert)]
        public static ushort ConvertToUInt16(ushort a) => a;

        [ExpressionOperation(ExpressionType.Convert)]
        public static ushort? ConvertToUInt16(ushort? a) => a;

        [ExpressionOperation(ExpressionType.Convert)]
        public static ushort ConvertToUInt16(int a) => Convert.ToUInt16(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static ushort? ConvertToUInt16(int? a) => a == null ? (ushort?)null : Convert.ToUInt16(a.Value);

        [ExpressionOperation(ExpressionType.Convert)]
        public static ushort ConvertToUInt16(uint a) => Convert.ToUInt16(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static ushort? ConvertToUInt16(uint? a) => a == null ? (ushort?)null : Convert.ToUInt16(a.Value);

        [ExpressionOperation(ExpressionType.Convert)]
        public static ushort ConvertToUInt16(long a) => Convert.ToUInt16(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static ushort? ConvertToUInt16(long? a) => a == null ? (ushort?)null : Convert.ToUInt16(a.Value);

        [ExpressionOperation(ExpressionType.Convert)]
        public static ushort ConvertToUInt16(ulong a) => Convert.ToUInt16(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static ushort? ConvertToUInt16(ulong? a) => a == null ? (ushort?)null : Convert.ToUInt16(a.Value);

        [ExpressionOperation(ExpressionType.Convert)]
        public static ushort ConvertToUInt16(float a) => Convert.ToUInt16(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static ushort? ConvertToUInt16(float? a) => a == null ? (ushort?)null : Convert.ToUInt16(a.Value);

        [ExpressionOperation(ExpressionType.Convert)]
        public static ushort ConvertToUInt16(double a) => Convert.ToUInt16(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static ushort? ConvertToUInt16(double? a) => a == null ? (ushort?)null : Convert.ToUInt16(a.Value);

        [ExpressionOperation(ExpressionType.Convert)]
        public static ushort ConvertToUInt16(decimal a) => Convert.ToUInt16(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static ushort? ConvertToUInt16(decimal? a) => a == null ? (ushort?)null : Convert.ToUInt16(a.Value);

        [ExpressionOperation(ExpressionType.Convert)]
        public static ushort ConvertToUInt16(string a) => Convert.ToUInt16(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static ushort ConvertToUInt16(object a) => Convert.ToUInt16(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static ushort? ConvertToNullableUInt16(object a) => a == null ? (ushort?)null : Convert.ToUInt16(a);

        #endregion ToUInt16

        #region ToInt32

        [ExpressionOperation(ExpressionType.Convert)]
        public static int ConvertToInt32(bool a) => Convert.ToInt32(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static int? ConvertToInt32(bool? a) => a == null ? (int?)null : Convert.ToInt32(a.Value);

        [ExpressionOperation(ExpressionType.Convert)]
        public static int ConvertToInt32(byte a) => Convert.ToInt32(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static int? ConvertToInt32(byte? a) => a == null ? (int?)null : Convert.ToInt32(a.Value);

        [ExpressionOperation(ExpressionType.Convert)]
        public static int ConvertToInt32(sbyte a) => Convert.ToInt32(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static int? ConvertToInt32(sbyte? a) => a == null ? (int?)null : Convert.ToInt32(a.Value);

        [ExpressionOperation(ExpressionType.Convert)]
        public static int ConvertToInt32(short a) => Convert.ToInt32(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static int? ConvertToInt32(short? a) => a == null ? (int?)null : Convert.ToInt32(a.Value);

        [ExpressionOperation(ExpressionType.Convert)]
        public static int ConvertToInt32(ushort a) => Convert.ToInt32(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static int? ConvertToInt32(ushort? a) => a == null ? (int?)null : Convert.ToInt32(a.Value);

        [ExpressionOperation(ExpressionType.Convert)]
        public static int ConvertToInt32(int a) => a;

        [ExpressionOperation(ExpressionType.Convert)]
        public static int? ConvertToInt32(int? a) => a;

        [ExpressionOperation(ExpressionType.Convert)]
        public static int ConvertToInt32(uint a) => Convert.ToInt32(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static int? ConvertToInt32(uint? a) => a == null ? (int?)null : Convert.ToInt32(a.Value);

        [ExpressionOperation(ExpressionType.Convert)]
        public static int ConvertToInt32(long a) => Convert.ToInt32(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static int? ConvertToInt32(long? a) => a == null ? (int?)null : Convert.ToInt32(a.Value);

        [ExpressionOperation(ExpressionType.Convert)]
        public static int ConvertToInt32(ulong a) => Convert.ToInt32(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static int? ConvertToInt32(ulong? a) => a == null ? (int?)null : Convert.ToInt32(a.Value);

        [ExpressionOperation(ExpressionType.Convert)]
        public static int ConvertToInt32(float a) => Convert.ToInt32(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static int? ConvertToInt32(float? a) => a == null ? (int?)null : Convert.ToInt32(a.Value);

        [ExpressionOperation(ExpressionType.Convert)]
        public static int ConvertToInt32(double a) => Convert.ToInt32(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static int? ConvertToInt32(double? a) => a == null ? (int?)null : Convert.ToInt32(a.Value);

        [ExpressionOperation(ExpressionType.Convert)]
        public static int ConvertToInt32(decimal a) => Convert.ToInt32(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static int? ConvertToInt32(decimal? a) => a == null ? (int?)null : Convert.ToInt32(a.Value);

        [ExpressionOperation(ExpressionType.Convert)]
        public static int ConvertToInt32(string a) => Convert.ToInt32(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static int ConvertToInt32(object a) => Convert.ToInt32(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static int? ConvertToNullableInt32(object a) => a == null ? (int?)null : Convert.ToInt32(a);

        #endregion ToInt32

        #region ToUInt32

        [ExpressionOperation(ExpressionType.Convert)]
        public static uint ConvertToUInt32(bool a) => Convert.ToUInt32(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static uint? ConvertToUInt32(bool? a) => a == null ? (uint?)null : Convert.ToUInt32(a.Value);

        [ExpressionOperation(ExpressionType.Convert)]
        public static uint ConvertToUInt32(byte a) => Convert.ToUInt32(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static uint? ConvertToUInt32(byte? a) => a == null ? (uint?)null : Convert.ToUInt32(a.Value);

        [ExpressionOperation(ExpressionType.Convert)]
        public static uint ConvertToUInt32(sbyte a) => Convert.ToUInt32(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static uint? ConvertToUInt32(sbyte? a) => a == null ? (uint?)null : Convert.ToUInt32(a.Value);

        [ExpressionOperation(ExpressionType.Convert)]
        public static uint ConvertToUInt32(short a) => Convert.ToUInt32(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static uint? ConvertToUInt32(short? a) => a == null ? (uint?)null : Convert.ToUInt32(a.Value);

        [ExpressionOperation(ExpressionType.Convert)]
        public static uint ConvertToUInt32(ushort a) => Convert.ToUInt32(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static uint? ConvertToUInt32(ushort? a) => a == null ? (uint?)null : Convert.ToUInt32(a.Value);

        [ExpressionOperation(ExpressionType.Convert)]
        public static uint ConvertToUInt32(int a) => Convert.ToUInt32(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static uint? ConvertToUInt32(int? a) => a == null ? (uint?)null : Convert.ToUInt32(a.Value);

        [ExpressionOperation(ExpressionType.Convert)]
        public static uint ConvertToUInt32(uint a) => a;

        [ExpressionOperation(ExpressionType.Convert)]
        public static uint? ConvertToUInt32(uint? a) => a;

        [ExpressionOperation(ExpressionType.Convert)]
        public static uint ConvertToUInt32(long a) => Convert.ToUInt32(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static uint? ConvertToUInt32(long? a) => a == null ? (uint?)null : Convert.ToUInt32(a.Value);

        [ExpressionOperation(ExpressionType.Convert)]
        public static uint ConvertToUInt32(ulong a) => Convert.ToUInt32(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static uint? ConvertToUInt32(ulong? a) => a == null ? (uint?)null : Convert.ToUInt32(a.Value);

        [ExpressionOperation(ExpressionType.Convert)]
        public static uint ConvertToUInt32(float a) => Convert.ToUInt32(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static uint? ConvertToUInt32(float? a) => a == null ? (uint?)null : Convert.ToUInt32(a.Value);

        [ExpressionOperation(ExpressionType.Convert)]
        public static uint ConvertToUInt32(double a) => Convert.ToUInt32(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static uint? ConvertToUInt32(double? a) => a == null ? (uint?)null : Convert.ToUInt32(a.Value);

        [ExpressionOperation(ExpressionType.Convert)]
        public static uint ConvertToUInt32(decimal a) => Convert.ToUInt32(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static uint? ConvertToUInt32(decimal? a) => a == null ? (uint?)null : Convert.ToUInt32(a.Value);

        [ExpressionOperation(ExpressionType.Convert)]
        public static uint ConvertToUInt32(string a) => Convert.ToUInt32(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static uint ConvertToUInt32(object a) => Convert.ToUInt32(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static uint? ConvertToNullableUInt32(object a) => a == null ? (uint?)null : Convert.ToUInt32(a);

        #endregion ToUInt32

        #region ToInt64

        [ExpressionOperation(ExpressionType.Convert)]
        public static long ConvertToInt64(bool a) => Convert.ToInt64(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static long? ConvertToInt64(bool? a) => a == null ? (long?)null : Convert.ToInt64(a.Value);

        [ExpressionOperation(ExpressionType.Convert)]
        public static long ConvertToInt64(byte a) => Convert.ToInt64(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static long? ConvertToInt64(byte? a) => a == null ? (long?)null : Convert.ToInt64(a.Value);

        [ExpressionOperation(ExpressionType.Convert)]
        public static long ConvertToInt64(sbyte a) => Convert.ToInt64(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static long? ConvertToInt64(sbyte? a) => a == null ? (long?)null : Convert.ToInt64(a.Value);

        [ExpressionOperation(ExpressionType.Convert)]
        public static long ConvertToInt64(short a) => Convert.ToInt64(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static long? ConvertToInt64(short? a) => a == null ? (long?)null : Convert.ToInt64(a.Value);

        [ExpressionOperation(ExpressionType.Convert)]
        public static long ConvertToInt64(ushort a) => Convert.ToInt64(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static long? ConvertToInt64(ushort? a) => a == null ? (long?)null : Convert.ToInt64(a.Value);

        [ExpressionOperation(ExpressionType.Convert)]
        public static long ConvertToInt64(int a) => Convert.ToInt64(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static long? ConvertToInt64(int? a) => a == null ? (long?)null : Convert.ToInt64(a.Value);

        [ExpressionOperation(ExpressionType.Convert)]
        public static long ConvertToInt64(uint a) => Convert.ToInt64(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static long? ConvertToInt64(uint? a) => a == null ? (long?)null : Convert.ToInt64(a.Value);

        [ExpressionOperation(ExpressionType.Convert)]
        public static long ConvertToInt64(long a) => a;

        [ExpressionOperation(ExpressionType.Convert)]
        public static long? ConvertToInt64(long? a) => a;

        [ExpressionOperation(ExpressionType.Convert)]
        public static long ConvertToInt64(ulong a) => Convert.ToInt64(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static long? ConvertToInt64(ulong? a) => a == null ? (long?)null : Convert.ToInt64(a.Value);

        [ExpressionOperation(ExpressionType.Convert)]
        public static long ConvertToInt64(float a) => Convert.ToInt64(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static long? ConvertToInt64(float? a) => a == null ? (long?)null : Convert.ToInt64(a.Value);

        [ExpressionOperation(ExpressionType.Convert)]
        public static long ConvertToInt64(double a) => Convert.ToInt64(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static long? ConvertToInt64(double? a) => a == null ? (long?)null : Convert.ToInt64(a.Value);

        [ExpressionOperation(ExpressionType.Convert)]
        public static long ConvertToInt64(decimal a) => Convert.ToInt64(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static long? ConvertToInt64(decimal? a) => a == null ? (long?)null : Convert.ToInt64(a.Value);

        [ExpressionOperation(ExpressionType.Convert)]
        public static long ConvertToInt64(string a) => Convert.ToInt64(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static long ConvertToInt64(object a) => Convert.ToInt64(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static long? ConvertToNullableInt64(object a) => a == null ? (long?)null : Convert.ToInt64(a);

        #endregion ToInt64

        #region ToUInt64

        [ExpressionOperation(ExpressionType.Convert)]
        public static ulong ConvertToUInt64(bool a) => Convert.ToUInt64(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static ulong? ConvertToUInt64(bool? a) => a == null ? (ulong?)null : Convert.ToUInt64(a.Value);

        [ExpressionOperation(ExpressionType.Convert)]
        public static ulong ConvertToUInt64(byte a) => Convert.ToUInt64(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static ulong? ConvertToUInt64(byte? a) => a == null ? (ulong?)null : Convert.ToUInt64(a.Value);

        [ExpressionOperation(ExpressionType.Convert)]
        public static ulong ConvertToUInt64(sbyte a) => Convert.ToUInt64(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static ulong? ConvertToUInt64(sbyte? a) => a == null ? (ulong?)null : Convert.ToUInt64(a.Value);

        [ExpressionOperation(ExpressionType.Convert)]
        public static ulong ConvertToUInt64(short a) => Convert.ToUInt64(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static ulong? ConvertToUInt64(short? a) => a == null ? (ulong?)null : Convert.ToUInt64(a.Value);

        [ExpressionOperation(ExpressionType.Convert)]
        public static ulong ConvertToUInt64(ushort a) => Convert.ToUInt64(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static ulong? ConvertToUInt64(ushort? a) => a == null ? (ulong?)null : Convert.ToUInt64(a.Value);

        [ExpressionOperation(ExpressionType.Convert)]
        public static ulong ConvertToUInt64(int a) => Convert.ToUInt64(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static ulong? ConvertToUInt64(int? a) => a == null ? (ulong?)null : Convert.ToUInt64(a.Value);

        [ExpressionOperation(ExpressionType.Convert)]
        public static ulong ConvertToUInt64(uint a) => Convert.ToUInt64(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static ulong? ConvertToUInt64(uint? a) => a == null ? (ulong?)null : Convert.ToUInt64(a.Value);

        [ExpressionOperation(ExpressionType.Convert)]
        public static ulong ConvertToUInt64(long a) => Convert.ToUInt64(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static ulong? ConvertToUInt64(long? a) => a == null ? (ulong?)null : Convert.ToUInt64(a.Value);

        [ExpressionOperation(ExpressionType.Convert)]
        public static ulong ConvertToUInt64(ulong a) => a;

        [ExpressionOperation(ExpressionType.Convert)]
        public static ulong? ConvertToUInt64(ulong? a) => a;

        [ExpressionOperation(ExpressionType.Convert)]
        public static ulong ConvertToUInt64(float a) => Convert.ToUInt64(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static ulong? ConvertToUInt64(float? a) => a == null ? (ulong?)null : Convert.ToUInt64(a.Value);

        [ExpressionOperation(ExpressionType.Convert)]
        public static ulong ConvertToUInt64(double a) => Convert.ToUInt64(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static ulong? ConvertToUInt64(double? a) => a == null ? (ulong?)null : Convert.ToUInt64(a.Value);

        [ExpressionOperation(ExpressionType.Convert)]
        public static ulong ConvertToUInt64(decimal a) => Convert.ToUInt64(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static ulong? ConvertToUInt64(decimal? a) => a == null ? (ulong?)null : Convert.ToUInt64(a.Value);

        [ExpressionOperation(ExpressionType.Convert)]
        public static ulong ConvertToUInt64(string a) => Convert.ToUInt64(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static ulong ConvertToUInt64(object a) => Convert.ToUInt64(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static ulong? ConvertToNullableUInt64(object a) => a == null ? (ulong?)null : Convert.ToUInt64(a);

        #endregion ToUInt64

        #region ToSingle

        [ExpressionOperation(ExpressionType.Convert)]
        public static float ConvertToSingle(bool a) => Convert.ToSingle(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static float? ConvertToSingle(bool? a) => a == null ? (float?)null : Convert.ToSingle(a.Value);

        [ExpressionOperation(ExpressionType.Convert)]
        public static float ConvertToSingle(byte a) => Convert.ToSingle(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static float? ConvertToSingle(byte? a) => a == null ? (float?)null : Convert.ToSingle(a.Value);

        [ExpressionOperation(ExpressionType.Convert)]
        public static float ConvertToSingle(sbyte a) => Convert.ToSingle(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static float? ConvertToSingle(sbyte? a) => a == null ? (float?)null : Convert.ToSingle(a.Value);

        [ExpressionOperation(ExpressionType.Convert)]
        public static float ConvertToSingle(short a) => Convert.ToSingle(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static float? ConvertToSingle(short? a) => a == null ? (float?)null : Convert.ToSingle(a.Value);

        [ExpressionOperation(ExpressionType.Convert)]
        public static float ConvertToSingle(ushort a) => Convert.ToSingle(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static float? ConvertToSingle(ushort? a) => a == null ? (float?)null : Convert.ToSingle(a.Value);

        [ExpressionOperation(ExpressionType.Convert)]
        public static float ConvertToSingle(int a) => Convert.ToSingle(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static float? ConvertToSingle(int? a) => a == null ? (float?)null : Convert.ToSingle(a.Value);

        [ExpressionOperation(ExpressionType.Convert)]
        public static float ConvertToSingle(uint a) => Convert.ToSingle(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static float? ConvertToSingle(uint? a) => a == null ? (float?)null : Convert.ToSingle(a.Value);

        [ExpressionOperation(ExpressionType.Convert)]
        public static float ConvertToSingle(long a) => Convert.ToSingle(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static float? ConvertToSingle(long? a) => a == null ? (float?)null : Convert.ToSingle(a.Value);

        [ExpressionOperation(ExpressionType.Convert)]
        public static float ConvertToSingle(ulong a) => Convert.ToSingle(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static float? ConvertToSingle(ulong? a) => a == null ? (float?)null : Convert.ToSingle(a.Value);

        [ExpressionOperation(ExpressionType.Convert)]
        public static float ConvertToSingle(float a) => a;

        [ExpressionOperation(ExpressionType.Convert)]
        public static float? ConvertToSingle(float? a) => a;

        [ExpressionOperation(ExpressionType.Convert)]
        public static float ConvertToSingle(double a) => Convert.ToSingle(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static float? ConvertToSingle(double? a) => a == null ? (float?)null : Convert.ToSingle(a.Value);

        [ExpressionOperation(ExpressionType.Convert)]
        public static float ConvertToSingle(decimal a) => Convert.ToSingle(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static float? ConvertToSingle(decimal? a) => a == null ? (float?)null : Convert.ToSingle(a.Value);

        [ExpressionOperation(ExpressionType.Convert)]
        public static float ConvertToSingle(string a) => Convert.ToSingle(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static float ConvertToSingle(object a) => Convert.ToSingle(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static float? ConvertToNullableSingle(object a) => a == null ? (float?)null : Convert.ToSingle(a);

        #endregion ToSingle

        #region ToDouble

        [ExpressionOperation(ExpressionType.Convert)]
        public static double ConvertToDouble(bool a) => Convert.ToDouble(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static double? ConvertToDouble(bool? a) => a == null ? (double?)null : Convert.ToDouble(a.Value);

        [ExpressionOperation(ExpressionType.Convert)]
        public static double ConvertToDouble(byte a) => Convert.ToDouble(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static double? ConvertToDouble(byte? a) => a == null ? (double?)null : Convert.ToDouble(a.Value);

        [ExpressionOperation(ExpressionType.Convert)]
        public static double ConvertToDouble(sbyte a) => Convert.ToDouble(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static double? ConvertToDouble(sbyte? a) => a == null ? (double?)null : Convert.ToDouble(a.Value);

        [ExpressionOperation(ExpressionType.Convert)]
        public static double ConvertToDouble(short a) => Convert.ToDouble(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static double? ConvertToDouble(short? a) => a == null ? (double?)null : Convert.ToDouble(a.Value);

        [ExpressionOperation(ExpressionType.Convert)]
        public static double ConvertToDouble(ushort a) => Convert.ToDouble(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static double? ConvertToDouble(ushort? a) => a == null ? (double?)null : Convert.ToDouble(a.Value);

        [ExpressionOperation(ExpressionType.Convert)]
        public static double ConvertToDouble(int a) => Convert.ToDouble(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static double? ConvertToDouble(int? a) => a == null ? (double?)null : Convert.ToDouble(a.Value);

        [ExpressionOperation(ExpressionType.Convert)]
        public static double ConvertToDouble(uint a) => Convert.ToDouble(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static double? ConvertToDouble(uint? a) => a == null ? (double?)null : Convert.ToDouble(a.Value);

        [ExpressionOperation(ExpressionType.Convert)]
        public static double ConvertToDouble(long a) => Convert.ToDouble(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static double? ConvertToDouble(long? a) => a == null ? (double?)null : Convert.ToDouble(a.Value);

        [ExpressionOperation(ExpressionType.Convert)]
        public static double ConvertToDouble(ulong a) => Convert.ToDouble(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static double? ConvertToDouble(ulong? a) => a == null ? (double?)null : Convert.ToDouble(a.Value);

        [ExpressionOperation(ExpressionType.Convert)]
        public static double ConvertToDouble(float a) => Convert.ToDouble(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static double? ConvertToDouble(float? a) => a == null ? (double?)null : Convert.ToDouble(a.Value);

        [ExpressionOperation(ExpressionType.Convert)]
        public static double ConvertToDouble(double a) => a;

        [ExpressionOperation(ExpressionType.Convert)]
        public static double? ConvertToDouble(double? a) => a;

        [ExpressionOperation(ExpressionType.Convert)]
        public static double ConvertToDouble(decimal a) => Convert.ToDouble(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static double? ConvertToDouble(decimal? a) => a == null ? (double?)null : Convert.ToDouble(a.Value);

        [ExpressionOperation(ExpressionType.Convert)]
        public static double ConvertToDouble(string a) => Convert.ToDouble(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static double ConvertToDouble(object a) => Convert.ToDouble(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static double? ConvertToNullableDouble(object a) => a == null ? (double?)null : Convert.ToDouble(a);

        #endregion ToDouble

        #region ToDecimal

        [ExpressionOperation(ExpressionType.Convert)]
        public static decimal ConvertToDecimal(bool a) => Convert.ToDecimal(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static decimal? ConvertToDecimal(bool? a) => a == null ? (decimal?)null : Convert.ToDecimal(a.Value);

        [ExpressionOperation(ExpressionType.Convert)]
        public static decimal ConvertToDecimal(byte a) => Convert.ToDecimal(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static decimal? ConvertToDecimal(byte? a) => a == null ? (decimal?)null : Convert.ToDecimal(a.Value);

        [ExpressionOperation(ExpressionType.Convert)]
        public static decimal ConvertToDecimal(sbyte a) => Convert.ToDecimal(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static decimal? ConvertToDecimal(sbyte? a) => a == null ? (decimal?)null : Convert.ToDecimal(a.Value);

        [ExpressionOperation(ExpressionType.Convert)]
        public static decimal ConvertToDecimal(short a) => Convert.ToDecimal(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static decimal? ConvertToDecimal(short? a) => a == null ? (decimal?)null : Convert.ToDecimal(a.Value);

        [ExpressionOperation(ExpressionType.Convert)]
        public static decimal ConvertToDecimal(ushort a) => Convert.ToDecimal(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static decimal? ConvertToDecimal(ushort? a) => a == null ? (decimal?)null : Convert.ToDecimal(a.Value);

        [ExpressionOperation(ExpressionType.Convert)]
        public static decimal ConvertToDecimal(int a) => Convert.ToDecimal(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static decimal? ConvertToDecimal(int? a) => a == null ? (decimal?)null : Convert.ToDecimal(a.Value);

        [ExpressionOperation(ExpressionType.Convert)]
        public static decimal ConvertToDecimal(uint a) => Convert.ToDecimal(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static decimal? ConvertToDecimal(uint? a) => a == null ? (decimal?)null : Convert.ToDecimal(a.Value);

        [ExpressionOperation(ExpressionType.Convert)]
        public static decimal ConvertToDecimal(long a) => Convert.ToDecimal(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static decimal? ConvertToDecimal(long? a) => a == null ? (decimal?)null : Convert.ToDecimal(a.Value);

        [ExpressionOperation(ExpressionType.Convert)]
        public static decimal ConvertToDecimal(ulong a) => Convert.ToDecimal(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static decimal? ConvertToDecimal(ulong? a) => a == null ? (decimal?)null : Convert.ToDecimal(a.Value);

        [ExpressionOperation(ExpressionType.Convert)]
        public static decimal ConvertToDecimal(float a) => Convert.ToDecimal(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static decimal? ConvertToDecimal(float? a) => a == null ? (decimal?)null : Convert.ToDecimal(a.Value);

        [ExpressionOperation(ExpressionType.Convert)]
        public static decimal ConvertToDecimal(double a) => Convert.ToDecimal(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static decimal? ConvertToDecimal(double? a) => a == null ? (decimal?)null : Convert.ToDecimal(a.Value);

        [ExpressionOperation(ExpressionType.Convert)]
        public static decimal ConvertToDecimal(decimal a) => a;

        [ExpressionOperation(ExpressionType.Convert)]
        public static decimal? ConvertToDecimal(decimal? a) => a;

        [ExpressionOperation(ExpressionType.Convert)]
        public static decimal ConvertToDecimal(string a) => Convert.ToDecimal(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static decimal ConvertToDecimal(object a) => Convert.ToDecimal(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static decimal? ConvertToNullableDecimal(object a) => a == null ? (decimal?)null : Convert.ToDecimal(a);

        #endregion ToDecimal

        #region ToString

        [ExpressionOperation(ExpressionType.Convert)]
        public static string ConvertToString(bool a) => Convert.ToString(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static string ConvertToString(bool? a) => a == null ? string.Empty : Convert.ToString(a.Value);

        [ExpressionOperation(ExpressionType.Convert)]
        public static string ConvertToString(byte a) => Convert.ToString(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static string ConvertToString(byte? a) => a == null ? string.Empty : Convert.ToString(a.Value);

        [ExpressionOperation(ExpressionType.Convert)]
        public static string ConvertToString(sbyte a) => Convert.ToString(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static string ConvertToString(sbyte? a) => a == null ? string.Empty : Convert.ToString(a.Value);

        [ExpressionOperation(ExpressionType.Convert)]
        public static string ConvertToString(short a) => Convert.ToString(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static string ConvertToString(short? a) => a == null ? string.Empty : Convert.ToString(a.Value);

        [ExpressionOperation(ExpressionType.Convert)]
        public static string ConvertToString(ushort a) => Convert.ToString(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static string ConvertToString(ushort? a) => a == null ? string.Empty : Convert.ToString(a.Value);

        [ExpressionOperation(ExpressionType.Convert)]
        public static string ConvertToString(int a) => Convert.ToString(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static string ConvertToString(int? a) => a == null ? string.Empty : Convert.ToString(a.Value);

        [ExpressionOperation(ExpressionType.Convert)]
        public static string ConvertToString(uint a) => Convert.ToString(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static string ConvertToString(uint? a) => a == null ? string.Empty : Convert.ToString(a.Value);

        [ExpressionOperation(ExpressionType.Convert)]
        public static string ConvertToString(long a) => Convert.ToString(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static string ConvertToString(long? a) => a == null ? string.Empty : Convert.ToString(a.Value);

        [ExpressionOperation(ExpressionType.Convert)]
        public static string ConvertToString(ulong a) => Convert.ToString(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static string ConvertToString(ulong? a) => a == null ? string.Empty : Convert.ToString(a.Value);

        [ExpressionOperation(ExpressionType.Convert)]
        public static string ConvertToString(float a) => Convert.ToString(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static string ConvertToString(float? a) => a == null ? string.Empty : Convert.ToString(a.Value);

        [ExpressionOperation(ExpressionType.Convert)]
        public static string ConvertToString(double a) => Convert.ToString(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static string ConvertToString(double? a) => a == null ? string.Empty : Convert.ToString(a.Value);

        [ExpressionOperation(ExpressionType.Convert)]
        public static string ConvertToString(decimal a) => Convert.ToString(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static string ConvertToString(decimal? a) => a == null ? string.Empty : Convert.ToString(a);

        [ExpressionOperation(ExpressionType.Convert)]
        public static string ConvertToString(string a) => a;

        [ExpressionOperation(ExpressionType.Convert)]
        public static string ConvertToString(object a) => a == null ? string.Empty : Convert.ToString(a);

        #endregion ToString

        #endregion Convert

        #region Decrement

        [ExpressionOperation(ExpressionType.Decrement)]
        public static byte Decrement(byte a) => (byte)(a - 1);

        [ExpressionOperation(ExpressionType.Decrement)]
        public static byte? Decrement(byte? a) => (byte?)(a - 1);

        [ExpressionOperation(ExpressionType.Decrement)]
        public static sbyte Decrement(sbyte a) => (sbyte)(a - 1);

        [ExpressionOperation(ExpressionType.Decrement)]
        public static sbyte? Decrement(sbyte? a) => (sbyte?)(a - 1);

        [ExpressionOperation(ExpressionType.Decrement)]
        public static short Decrement(short a) => (short)(a - 1);

        [ExpressionOperation(ExpressionType.Decrement)]
        public static short? Decrement(short? a) => (short?)(a - 1);

        [ExpressionOperation(ExpressionType.Decrement)]
        public static ushort Decrement(ushort a) => (ushort)(a - 1);

        [ExpressionOperation(ExpressionType.Decrement)]
        public static ushort? Decrement(ushort? a) => (ushort?)(a - 1);

        [ExpressionOperation(ExpressionType.Decrement)]
        public static int Decrement(int a) => a - 1;

        [ExpressionOperation(ExpressionType.Decrement)]
        public static int? Decrement(int? a) => a - 1;

        [ExpressionOperation(ExpressionType.Decrement)]
        public static uint Decrement(uint a) => a - 1;

        [ExpressionOperation(ExpressionType.Decrement)]
        public static uint? Decrement(uint? a) => a - 1;

        [ExpressionOperation(ExpressionType.Decrement)]
        public static long Decrement(long a) => a - 1;

        [ExpressionOperation(ExpressionType.Decrement)]
        public static long? Decrement(long? a) => a - 1;

        [ExpressionOperation(ExpressionType.Decrement)]
        public static ulong Decrement(ulong a) => a - 1;

        [ExpressionOperation(ExpressionType.Decrement)]
        public static ulong? Decrement(ulong? a) => a - 1;

        [ExpressionOperation(ExpressionType.Decrement)]
        public static float Decrement(float a) => a - 1;

        [ExpressionOperation(ExpressionType.Decrement)]
        public static float? Decrement(float? a) => a - 1;

        [ExpressionOperation(ExpressionType.Decrement)]
        public static double Decrement(double a) => a - 1;

        [ExpressionOperation(ExpressionType.Decrement)]
        public static double? Decrement(double? a) => a - 1;

        [ExpressionOperation(ExpressionType.Decrement)]
        public static decimal Decrement(decimal a) => a - 1;

        [ExpressionOperation(ExpressionType.Decrement)]
        public static decimal? Decrement(decimal? a) => a - 1;

        #endregion Decrement

        #region Divide

        [ExpressionOperation(ExpressionType.Divide)]
        public static byte Divide(byte a, byte b) => (byte)(a / b);

        [ExpressionOperation(ExpressionType.Divide)]
        public static byte? Divide(byte? a, byte? b) => (byte?)(a / b);

        [ExpressionOperation(ExpressionType.Divide)]
        public static sbyte Divide(sbyte a, sbyte b) => (sbyte)(a / b);

        [ExpressionOperation(ExpressionType.Divide)]
        public static sbyte? Divide(sbyte? a, sbyte? b) => (sbyte?)(a / b);

        [ExpressionOperation(ExpressionType.Divide)]
        public static short Divide(short a, short b) => (short)(a / b);

        [ExpressionOperation(ExpressionType.Divide)]
        public static short? Divide(short? a, short? b) => (short?)(a / b);

        [ExpressionOperation(ExpressionType.Divide)]
        public static ushort Divide(ushort a, ushort b) => (ushort)(a / b);

        [ExpressionOperation(ExpressionType.Divide)]
        public static ushort? Divide(ushort? a, ushort? b) => (ushort?)(a / b);

        [ExpressionOperation(ExpressionType.Divide)]
        public static int Divide(int a, int b) => a / b;

        [ExpressionOperation(ExpressionType.Divide)]
        public static int? Divide(int? a, int? b) => a / b;

        [ExpressionOperation(ExpressionType.Divide)]
        public static uint Divide(uint a, uint b) => a / b;

        [ExpressionOperation(ExpressionType.Divide)]
        public static uint? Divide(uint? a, uint? b) => a / b;

        [ExpressionOperation(ExpressionType.Divide)]
        public static long Divide(long a, long b) => a / b;

        [ExpressionOperation(ExpressionType.Divide)]
        public static long? Divide(long? a, long? b) => a / b;

        [ExpressionOperation(ExpressionType.Divide)]
        public static ulong Divide(ulong a, ulong b) => a / b;

        [ExpressionOperation(ExpressionType.Divide)]
        public static ulong? Divide(ulong? a, ulong? b) => a / b;

        [ExpressionOperation(ExpressionType.Divide)]
        public static float Divide(float a, float b) => a / b;

        [ExpressionOperation(ExpressionType.Divide)]
        public static float? Divide(float? a, float? b) => a / b;

        [ExpressionOperation(ExpressionType.Divide)]
        public static double Divide(double a, double b) => a / b;

        [ExpressionOperation(ExpressionType.Divide)]
        public static double? Divide(double? a, double? b) => a / b;

        [ExpressionOperation(ExpressionType.Divide)]
        public static decimal Divide(decimal a, decimal b) => a / b;

        [ExpressionOperation(ExpressionType.Divide)]
        public static decimal? Divide(decimal? a, decimal? b) => a / b;

        #endregion Divide

        #region Equal

        [ExpressionOperation(ExpressionType.Equal)]
        public static bool Equal(byte a, byte b) => a == b;

        [ExpressionOperation(ExpressionType.Equal)]
        public static bool Equal(byte? a, byte? b) => a == b;

        [ExpressionOperation(ExpressionType.Equal)]
        public static bool Equal(sbyte a, sbyte b) => a == b;

        [ExpressionOperation(ExpressionType.Equal)]
        public static bool Equal(sbyte? a, sbyte? b) => a == b;

        [ExpressionOperation(ExpressionType.Equal)]
        public static bool Equal(short a, short b) => a == b;

        [ExpressionOperation(ExpressionType.Equal)]
        public static bool Equal(short? a, short? b) => a == b;

        [ExpressionOperation(ExpressionType.Equal)]
        public static bool Equal(int a, int b) => a == b;

        [ExpressionOperation(ExpressionType.Equal)]
        public static bool Equal(int? a, int? b) => a == b;

        [ExpressionOperation(ExpressionType.Equal)]
        public static bool Equal(long a, long b) => a == b;

        [ExpressionOperation(ExpressionType.Equal)]
        public static bool Equal(long? a, long? b) => a == b;

        [ExpressionOperation(ExpressionType.Equal)]
        public static bool Equal(float a, float b) => a == b;

        [ExpressionOperation(ExpressionType.Equal)]
        public static bool Equal(float? a, float? b) => a == b;

        [ExpressionOperation(ExpressionType.Equal)]
        public static bool Equal(double a, double b) => a == b;

        [ExpressionOperation(ExpressionType.Equal)]
        public static bool Equal(double? a, double? b) => a == b;

        [ExpressionOperation(ExpressionType.Equal)]
        public static bool Equal(decimal a, decimal b) => a == b;

        [ExpressionOperation(ExpressionType.Equal)]
        public static bool Equal(decimal? a, decimal? b) => a == b;

        [ExpressionOperation(ExpressionType.Equal)]
        public static bool Equal(DateTime a, DateTime b) => a == b;

        [ExpressionOperation(ExpressionType.Equal)]
        public static bool Equal(DateTime? a, DateTime? b) => a == b;

        [ExpressionOperation(ExpressionType.Equal)]
        public static bool Equal(TimeSpan a, TimeSpan b) => a == b;

        [ExpressionOperation(ExpressionType.Equal)]
        public static bool Equal(TimeSpan? a, TimeSpan? b) => a == b;

        [ExpressionOperation(ExpressionType.Equal)]
        public static bool Equal(object a, object b) => a == b;

        #endregion Equal

        #region ExclusiveOr

        [ExpressionOperation(ExpressionType.ExclusiveOr)]
        public static bool ExclusiveOr(bool a, bool b) => a ^ b;

        [ExpressionOperation(ExpressionType.ExclusiveOr)]
        public static bool? ExclusiveOr(bool? a, bool? b) => a ^ b;

        [ExpressionOperation(ExpressionType.ExclusiveOr)]
        public static byte ExclusiveOr(byte a, byte b) => (byte)(a ^ b);

        [ExpressionOperation(ExpressionType.ExclusiveOr)]
        public static byte? ExclusiveOr(byte? a, byte? b) => (byte?)(a ^ b);

        [ExpressionOperation(ExpressionType.ExclusiveOr)]
        public static sbyte ExclusiveOr(sbyte a, sbyte b) => (sbyte)(a ^ b);

        [ExpressionOperation(ExpressionType.ExclusiveOr)]
        public static sbyte? ExclusiveOr(sbyte? a, sbyte? b) => (sbyte?)(a ^ b);

        [ExpressionOperation(ExpressionType.ExclusiveOr)]
        public static short ExclusiveOr(short a, short b) => (short)(a ^ b);

        [ExpressionOperation(ExpressionType.ExclusiveOr)]
        public static short? ExclusiveOr(short? a, short? b) => (short?)(a ^ b);

        [ExpressionOperation(ExpressionType.ExclusiveOr)]
        public static ushort ExclusiveOr(ushort a, ushort b) => (ushort)(a ^ b);

        [ExpressionOperation(ExpressionType.ExclusiveOr)]
        public static ushort? ExclusiveOr(ushort? a, ushort? b) => (ushort?)(a ^ b);

        [ExpressionOperation(ExpressionType.ExclusiveOr)]
        public static int ExclusiveOr(int a, int b) => a ^ b;

        [ExpressionOperation(ExpressionType.ExclusiveOr)]
        public static int? ExclusiveOr(int? a, int? b) => a ^ b;

        [ExpressionOperation(ExpressionType.ExclusiveOr)]
        public static uint ExclusiveOr(uint a, uint b) => a ^ b;

        [ExpressionOperation(ExpressionType.ExclusiveOr)]
        public static uint? ExclusiveOr(uint? a, uint? b) => a ^ b;

        [ExpressionOperation(ExpressionType.ExclusiveOr)]
        public static long ExclusiveOr(long a, long b) => a ^ b;

        [ExpressionOperation(ExpressionType.ExclusiveOr)]
        public static long? ExclusiveOr(long? a, long? b) => a ^ b;

        [ExpressionOperation(ExpressionType.ExclusiveOr)]
        public static ulong ExclusiveOr(ulong a, ulong b) => a ^ b;

        [ExpressionOperation(ExpressionType.ExclusiveOr)]
        public static ulong? ExclusiveOr(ulong? a, ulong? b) => a ^ b;

        #endregion ExclusiveOr

        #region GreaterThan

        [ExpressionOperation(ExpressionType.GreaterThan)]
        public static bool GreaterThan(byte a, byte b) => a > b;

        [ExpressionOperation(ExpressionType.GreaterThan)]
        public static bool GreaterThan(byte? a, byte? b) => a > b;

        [ExpressionOperation(ExpressionType.GreaterThan)]
        public static bool GreaterThan(sbyte a, sbyte b) => a > b;

        [ExpressionOperation(ExpressionType.GreaterThan)]
        public static bool GreaterThan(sbyte? a, sbyte? b) => a > b;

        [ExpressionOperation(ExpressionType.GreaterThan)]
        public static bool GreaterThan(short a, short b) => a > b;

        [ExpressionOperation(ExpressionType.GreaterThan)]
        public static bool GreaterThan(short? a, short? b) => a > b;

        [ExpressionOperation(ExpressionType.GreaterThan)]
        public static bool GreaterThan(int a, int b) => a > b;

        [ExpressionOperation(ExpressionType.GreaterThan)]
        public static bool GreaterThan(int? a, int? b) => a > b;

        [ExpressionOperation(ExpressionType.GreaterThan)]
        public static bool GreaterThan(long a, long b) => a > b;

        [ExpressionOperation(ExpressionType.GreaterThan)]
        public static bool GreaterThan(long? a, long? b) => a > b;

        [ExpressionOperation(ExpressionType.GreaterThan)]
        public static bool GreaterThan(float a, float b) => a > b;

        [ExpressionOperation(ExpressionType.GreaterThan)]
        public static bool GreaterThan(float? a, float? b) => a > b;

        [ExpressionOperation(ExpressionType.GreaterThan)]
        public static bool GreaterThan(double a, double b) => a > b;

        [ExpressionOperation(ExpressionType.GreaterThan)]
        public static bool GreaterThan(double? a, double? b) => a > b;

        [ExpressionOperation(ExpressionType.GreaterThan)]
        public static bool GreaterThan(decimal a, decimal b) => a > b;

        [ExpressionOperation(ExpressionType.GreaterThan)]
        public static bool GreaterThan(decimal? a, decimal? b) => a > b;

        [ExpressionOperation(ExpressionType.GreaterThan)]
        public static bool GreaterThan(DateTime a, DateTime b) => a > b;

        [ExpressionOperation(ExpressionType.GreaterThan)]
        public static bool GreaterThan(DateTime? a, DateTime? b) => a > b;

        [ExpressionOperation(ExpressionType.GreaterThan)]
        public static bool GreaterThan(TimeSpan a, TimeSpan b) => a > b;

        [ExpressionOperation(ExpressionType.GreaterThan)]
        public static bool GreaterThan(TimeSpan? a, TimeSpan? b) => a > b;

        #endregion GreaterThan

        #region GreaterThanOrEqual

        [ExpressionOperation(ExpressionType.GreaterThanOrEqual)]
        public static bool GreaterThanOrEqual(byte a, byte b) => a >= b;

        [ExpressionOperation(ExpressionType.GreaterThanOrEqual)]
        public static bool GreaterThanOrEqual(byte? a, byte? b) => a >= b;

        [ExpressionOperation(ExpressionType.GreaterThanOrEqual)]
        public static bool GreaterThanOrEqual(sbyte a, sbyte b) => a >= b;

        [ExpressionOperation(ExpressionType.GreaterThanOrEqual)]
        public static bool GreaterThanOrEqual(sbyte? a, sbyte? b) => a >= b;

        [ExpressionOperation(ExpressionType.GreaterThanOrEqual)]
        public static bool GreaterThanOrEqual(short a, short b) => a >= b;

        [ExpressionOperation(ExpressionType.GreaterThanOrEqual)]
        public static bool GreaterThanOrEqual(short? a, short? b) => a >= b;

        [ExpressionOperation(ExpressionType.GreaterThanOrEqual)]
        public static bool GreaterThanOrEqual(int a, int b) => a >= b;

        [ExpressionOperation(ExpressionType.GreaterThanOrEqual)]
        public static bool GreaterThanOrEqual(int? a, int? b) => a >= b;

        [ExpressionOperation(ExpressionType.GreaterThanOrEqual)]
        public static bool GreaterThanOrEqual(long a, long b) => a >= b;

        [ExpressionOperation(ExpressionType.GreaterThanOrEqual)]
        public static bool GreaterThanOrEqual(long? a, long? b) => a >= b;

        [ExpressionOperation(ExpressionType.GreaterThanOrEqual)]
        public static bool GreaterThanOrEqual(float a, float b) => a >= b;

        [ExpressionOperation(ExpressionType.GreaterThanOrEqual)]
        public static bool GreaterThanOrEqual(float? a, float? b) => a >= b;

        [ExpressionOperation(ExpressionType.GreaterThanOrEqual)]
        public static bool GreaterThanOrEqual(double a, double b) => a >= b;

        [ExpressionOperation(ExpressionType.GreaterThanOrEqual)]
        public static bool GreaterThanOrEqual(double? a, double? b) => a >= b;

        [ExpressionOperation(ExpressionType.GreaterThanOrEqual)]
        public static bool GreaterThanOrEqual(decimal a, decimal b) => a >= b;

        [ExpressionOperation(ExpressionType.GreaterThanOrEqual)]
        public static bool GreaterThanOrEqual(decimal? a, decimal? b) => a >= b;

        [ExpressionOperation(ExpressionType.GreaterThanOrEqual)]
        public static bool GreaterThanOrEqual(DateTime a, DateTime b) => a >= b;

        [ExpressionOperation(ExpressionType.GreaterThanOrEqual)]
        public static bool GreaterThanOrEqual(DateTime? a, DateTime? b) => a >= b;

        [ExpressionOperation(ExpressionType.GreaterThanOrEqual)]
        public static bool GreaterThanOrEqual(TimeSpan a, TimeSpan b) => a >= b;

        [ExpressionOperation(ExpressionType.GreaterThanOrEqual)]
        public static bool GreaterThanOrEqual(TimeSpan? a, TimeSpan? b) => a >= b;

        #endregion GreaterThanOrEqual

        #region Increment

        [ExpressionOperation(ExpressionType.Increment)]
        public static byte Increment(byte a) => (byte)(a + 1);

        [ExpressionOperation(ExpressionType.Increment)]
        public static byte? Increment(byte? a) => (byte?)(a + 1);

        [ExpressionOperation(ExpressionType.Increment)]
        public static sbyte Increment(sbyte a) => (sbyte)(a + 1);

        [ExpressionOperation(ExpressionType.Increment)]
        public static sbyte? Increment(sbyte? a) => (sbyte?)(a + 1);

        [ExpressionOperation(ExpressionType.Increment)]
        public static short Increment(short a) => (short)(a + 1);

        [ExpressionOperation(ExpressionType.Increment)]
        public static short? Increment(short? a) => (short?)(a + 1);

        [ExpressionOperation(ExpressionType.Increment)]
        public static ushort Increment(ushort a) => (ushort)(a + 1);

        [ExpressionOperation(ExpressionType.Increment)]
        public static ushort? Increment(ushort? a) => (ushort?)(a + 1);

        [ExpressionOperation(ExpressionType.Increment)]
        public static int Increment(int a) => a + 1;

        [ExpressionOperation(ExpressionType.Increment)]
        public static int? Increment(int? a) => a + 1;

        [ExpressionOperation(ExpressionType.Increment)]
        public static uint Increment(uint a) => a + 1;

        [ExpressionOperation(ExpressionType.Increment)]
        public static uint? Increment(uint? a) => a + 1;

        [ExpressionOperation(ExpressionType.Increment)]
        public static long Increment(long a) => a + 1;

        [ExpressionOperation(ExpressionType.Increment)]
        public static long? Increment(long? a) => a + 1;

        [ExpressionOperation(ExpressionType.Increment)]
        public static ulong Increment(ulong a) => a + 1;

        [ExpressionOperation(ExpressionType.Increment)]
        public static ulong? Increment(ulong? a) => a + 1;

        [ExpressionOperation(ExpressionType.Increment)]
        public static float Increment(float a) => a + 1;

        [ExpressionOperation(ExpressionType.Increment)]
        public static float? Increment(float? a) => a + 1;

        [ExpressionOperation(ExpressionType.Increment)]
        public static double Increment(double a) => a + 1;

        [ExpressionOperation(ExpressionType.Increment)]
        public static double? Increment(double? a) => a + 1;

        [ExpressionOperation(ExpressionType.Increment)]
        public static decimal Increment(decimal a) => a + 1;

        [ExpressionOperation(ExpressionType.Increment)]
        public static decimal? Increment(decimal? a) => a + 1;

        #endregion Increment

        #region LeftShift

        [ExpressionOperation(ExpressionType.LeftShift)]
        public static byte LeftShift(byte a, byte b) => (byte)(a << b);

        [ExpressionOperation(ExpressionType.LeftShift)]
        public static byte? LeftShift(byte? a, byte? b) => (byte?)(a << b);

        [ExpressionOperation(ExpressionType.LeftShift)]
        public static sbyte LeftShift(sbyte a, sbyte b) => (sbyte)(a << b);

        [ExpressionOperation(ExpressionType.LeftShift)]
        public static sbyte? LeftShift(sbyte? a, sbyte? b) => (sbyte?)(a << b);

        [ExpressionOperation(ExpressionType.LeftShift)]
        public static short LeftShift(short a, short b) => (short)(a << b);

        [ExpressionOperation(ExpressionType.LeftShift)]
        public static short? LeftShift(short? a, short? b) => (short?)(a << b);

        [ExpressionOperation(ExpressionType.LeftShift)]
        public static ushort LeftShift(ushort a, ushort b) => (ushort)(a << b);

        [ExpressionOperation(ExpressionType.LeftShift)]
        public static ushort? LeftShift(ushort? a, ushort? b) => (ushort?)(a << b);

        [ExpressionOperation(ExpressionType.LeftShift)]
        public static int LeftShift(int a, int b) => a << b;

        [ExpressionOperation(ExpressionType.LeftShift)]
        public static int? LeftShift(int? a, int? b) => a << b;

        [ExpressionOperation(ExpressionType.LeftShift)]
        public static uint LeftShift(uint a, int b) => a << b;

        [ExpressionOperation(ExpressionType.LeftShift)]
        public static uint? LeftShift(uint? a, int? b) => a << b;

        [ExpressionOperation(ExpressionType.LeftShift)]
        public static long LeftShift(long a, int b) => a << b;

        [ExpressionOperation(ExpressionType.LeftShift)]
        public static long? LeftShift(long? a, int? b) => a << b;

        [ExpressionOperation(ExpressionType.LeftShift)]
        public static ulong LeftShift(ulong a, int b) => a << b;

        [ExpressionOperation(ExpressionType.LeftShift)]
        public static ulong? LeftShift(ulong? a, int? b) => a << b;

        #endregion LeftShift

        #region LessThan

        [ExpressionOperation(ExpressionType.LessThan)]
        public static bool LessThan(byte a, byte b) => a < b;

        [ExpressionOperation(ExpressionType.LessThan)]
        public static bool LessThan(byte? a, byte? b) => a < b;

        [ExpressionOperation(ExpressionType.LessThan)]
        public static bool LessThan(sbyte a, sbyte b) => a < b;

        [ExpressionOperation(ExpressionType.LessThan)]
        public static bool LessThan(sbyte? a, sbyte? b) => a < b;

        [ExpressionOperation(ExpressionType.LessThan)]
        public static bool LessThan(short a, short b) => a < b;

        [ExpressionOperation(ExpressionType.LessThan)]
        public static bool LessThan(short? a, short? b) => a < b;

        [ExpressionOperation(ExpressionType.LessThan)]
        public static bool LessThan(int a, int b) => a < b;

        [ExpressionOperation(ExpressionType.LessThan)]
        public static bool LessThan(int? a, int? b) => a < b;

        [ExpressionOperation(ExpressionType.LessThan)]
        public static bool LessThan(long a, long b) => a < b;

        [ExpressionOperation(ExpressionType.LessThan)]
        public static bool LessThan(long? a, long? b) => a < b;

        [ExpressionOperation(ExpressionType.LessThan)]
        public static bool LessThan(float a, float b) => a < b;

        [ExpressionOperation(ExpressionType.LessThan)]
        public static bool LessThan(float? a, float? b) => a < b;

        [ExpressionOperation(ExpressionType.LessThan)]
        public static bool LessThan(double a, double b) => a < b;

        [ExpressionOperation(ExpressionType.LessThan)]
        public static bool LessThan(double? a, double? b) => a < b;

        [ExpressionOperation(ExpressionType.LessThan)]
        public static bool LessThan(decimal a, decimal b) => a < b;

        [ExpressionOperation(ExpressionType.LessThan)]
        public static bool LessThan(decimal? a, decimal? b) => a < b;

        [ExpressionOperation(ExpressionType.LessThan)]
        public static bool LessThan(DateTime a, DateTime b) => a < b;

        [ExpressionOperation(ExpressionType.LessThan)]
        public static bool LessThan(DateTime? a, DateTime? b) => a < b;

        [ExpressionOperation(ExpressionType.LessThan)]
        public static bool LessThan(TimeSpan a, TimeSpan b) => a < b;

        [ExpressionOperation(ExpressionType.LessThan)]
        public static bool LessThan(TimeSpan? a, TimeSpan? b) => a < b;

        #endregion LessThan

        #region LessThanOrEqual

        [ExpressionOperation(ExpressionType.LessThanOrEqual)]
        public static bool LessThanOrEqual(byte a, byte b) => a <= b;

        [ExpressionOperation(ExpressionType.LessThanOrEqual)]
        public static bool LessThanOrEqual(byte? a, byte? b) => a <= b;

        [ExpressionOperation(ExpressionType.LessThanOrEqual)]
        public static bool LessThanOrEqual(sbyte a, sbyte b) => a <= b;

        [ExpressionOperation(ExpressionType.LessThanOrEqual)]
        public static bool LessThanOrEqual(sbyte? a, sbyte? b) => a <= b;

        [ExpressionOperation(ExpressionType.LessThanOrEqual)]
        public static bool LessThanOrEqual(short a, short b) => a <= b;

        [ExpressionOperation(ExpressionType.LessThanOrEqual)]
        public static bool LessThanOrEqual(short? a, short? b) => a <= b;

        [ExpressionOperation(ExpressionType.LessThanOrEqual)]
        public static bool LessThanOrEqual(int a, int b) => a <= b;

        [ExpressionOperation(ExpressionType.LessThanOrEqual)]
        public static bool LessThanOrEqual(int? a, int? b) => a <= b;

        [ExpressionOperation(ExpressionType.LessThanOrEqual)]
        public static bool LessThanOrEqual(long a, long b) => a <= b;

        [ExpressionOperation(ExpressionType.LessThanOrEqual)]
        public static bool LessThanOrEqual(long? a, long? b) => a <= b;

        [ExpressionOperation(ExpressionType.LessThanOrEqual)]
        public static bool LessThanOrEqual(float a, float b) => a <= b;

        [ExpressionOperation(ExpressionType.LessThanOrEqual)]
        public static bool LessThanOrEqual(float? a, float? b) => a <= b;

        [ExpressionOperation(ExpressionType.LessThanOrEqual)]
        public static bool LessThanOrEqual(double a, double b) => a <= b;

        [ExpressionOperation(ExpressionType.LessThanOrEqual)]
        public static bool LessThanOrEqual(double? a, double? b) => a <= b;

        [ExpressionOperation(ExpressionType.LessThanOrEqual)]
        public static bool LessThanOrEqual(decimal a, decimal b) => a <= b;

        [ExpressionOperation(ExpressionType.LessThanOrEqual)]
        public static bool LessThanOrEqual(decimal? a, decimal? b) => a <= b;

        [ExpressionOperation(ExpressionType.LessThanOrEqual)]
        public static bool LessThanOrEqual(DateTime a, DateTime b) => a <= b;

        [ExpressionOperation(ExpressionType.LessThanOrEqual)]
        public static bool LessThanOrEqual(DateTime? a, DateTime? b) => a <= b;

        [ExpressionOperation(ExpressionType.LessThanOrEqual)]
        public static bool LessThanOrEqual(TimeSpan a, TimeSpan b) => a <= b;

        [ExpressionOperation(ExpressionType.LessThanOrEqual)]
        public static bool LessThanOrEqual(TimeSpan? a, TimeSpan? b) => a <= b;

        #endregion LessThanOrEqual

        #region Modulo

        [ExpressionOperation(ExpressionType.Modulo)]
        public static byte Modulo(byte a, byte b) => (byte)(a % b);

        [ExpressionOperation(ExpressionType.Modulo)]
        public static byte? Modulo(byte? a, byte? b) => (byte?)(a % b);

        [ExpressionOperation(ExpressionType.Modulo)]
        public static sbyte Modulo(sbyte a, sbyte b) => (sbyte)(a % b);

        [ExpressionOperation(ExpressionType.Modulo)]
        public static sbyte? Modulo(sbyte? a, sbyte? b) => (sbyte?)(a % b);

        [ExpressionOperation(ExpressionType.Modulo)]
        public static short Modulo(short a, short b) => (short)(a % b);

        [ExpressionOperation(ExpressionType.Modulo)]
        public static short? Modulo(short? a, short? b) => (short?)(a % b);

        [ExpressionOperation(ExpressionType.Modulo)]
        public static ushort Modulo(ushort a, ushort b) => (ushort)(a % b);

        [ExpressionOperation(ExpressionType.Modulo)]
        public static ushort? Modulo(ushort? a, ushort? b) => (ushort?)(a % b);

        [ExpressionOperation(ExpressionType.Modulo)]
        public static int Modulo(int a, int b) => a % b;

        [ExpressionOperation(ExpressionType.Modulo)]
        public static int? Modulo(int? a, int? b) => a % b;

        [ExpressionOperation(ExpressionType.Modulo)]
        public static uint Modulo(uint a, uint b) => a % b;

        [ExpressionOperation(ExpressionType.Modulo)]
        public static uint? Modulo(uint? a, uint? b) => a % b;

        [ExpressionOperation(ExpressionType.Modulo)]
        public static long Modulo(long a, long b) => a % b;

        [ExpressionOperation(ExpressionType.Modulo)]
        public static long? Modulo(long? a, long? b) => a % b;

        [ExpressionOperation(ExpressionType.Modulo)]
        public static ulong Modulo(ulong a, ulong b) => a % b;

        [ExpressionOperation(ExpressionType.Modulo)]
        public static ulong? Modulo(ulong? a, ulong? b) => a % b;

        [ExpressionOperation(ExpressionType.Modulo)]
        public static float Modulo(float a, float b) => a % b;

        [ExpressionOperation(ExpressionType.Modulo)]
        public static float? Modulo(float? a, float? b) => a % b;

        [ExpressionOperation(ExpressionType.Modulo)]
        public static double Modulo(double a, double b) => a % b;

        [ExpressionOperation(ExpressionType.Modulo)]
        public static double? Modulo(double? a, double? b) => a % b;

        [ExpressionOperation(ExpressionType.Modulo)]
        public static decimal Modulo(decimal a, decimal b) => a % b;

        [ExpressionOperation(ExpressionType.Modulo)]
        public static decimal? Modulo(decimal? a, decimal? b) => a % b;

        #endregion Modulo

        #region Multiply

        [ExpressionOperation(ExpressionType.Multiply)]
        public static byte Multiply(byte a, byte b) => (byte)(a * b);

        [ExpressionOperation(ExpressionType.Multiply)]
        public static byte? Multiply(byte? a, byte? b) => (byte?)(a * b);

        [ExpressionOperation(ExpressionType.Multiply)]
        public static sbyte Multiply(sbyte a, sbyte b) => (sbyte)(a * b);

        [ExpressionOperation(ExpressionType.Multiply)]
        public static sbyte? Multiply(sbyte? a, sbyte? b) => (sbyte?)(a * b);

        [ExpressionOperation(ExpressionType.Multiply)]
        public static short Multiply(short a, short b) => (short)(a * b);

        [ExpressionOperation(ExpressionType.Multiply)]
        public static short? Multiply(short? a, short? b) => (short?)(a * b);

        [ExpressionOperation(ExpressionType.Multiply)]
        public static ushort Multiply(ushort a, ushort b) => (ushort)(a * b);

        [ExpressionOperation(ExpressionType.Multiply)]
        public static ushort? Multiply(ushort? a, ushort? b) => (ushort?)(a * b);

        [ExpressionOperation(ExpressionType.Multiply)]
        public static int Multiply(int a, int b) => a * b;

        [ExpressionOperation(ExpressionType.Multiply)]
        public static int? Multiply(int? a, int? b) => a * b;

        [ExpressionOperation(ExpressionType.Multiply)]
        public static uint Multiply(uint a, uint b) => a * b;

        [ExpressionOperation(ExpressionType.Multiply)]
        public static uint? Multiply(uint? a, uint? b) => a * b;

        [ExpressionOperation(ExpressionType.Multiply)]
        public static long Multiply(long a, long b) => a * b;

        [ExpressionOperation(ExpressionType.Multiply)]
        public static long? Multiply(long? a, long? b) => a * b;

        [ExpressionOperation(ExpressionType.Multiply)]
        public static ulong Multiply(ulong a, ulong b) => a * b;

        [ExpressionOperation(ExpressionType.Multiply)]
        public static ulong? Multiply(ulong? a, ulong? b) => a * b;

        [ExpressionOperation(ExpressionType.Multiply)]
        public static float Multiply(float a, float b) => a * b;

        [ExpressionOperation(ExpressionType.Multiply)]
        public static float? Multiply(float? a, float? b) => a * b;

        [ExpressionOperation(ExpressionType.Multiply)]
        public static double Multiply(double a, double b) => a * b;

        [ExpressionOperation(ExpressionType.Multiply)]
        public static double? Multiply(double? a, double? b) => a * b;

        [ExpressionOperation(ExpressionType.Multiply)]
        public static decimal Multiply(decimal a, decimal b) => a * b;

        [ExpressionOperation(ExpressionType.Multiply)]
        public static decimal? Multiply(decimal? a, decimal? b) => a * b;

        #endregion Multiply

        #region MultiplyChecked

        [ExpressionOperation(ExpressionType.MultiplyChecked)]
        public static byte MultiplyChecked(byte a, byte b) => checked((byte)(a * b));

        [ExpressionOperation(ExpressionType.MultiplyChecked)]
        public static byte? MultiplyChecked(byte? a, byte? b) => checked((byte?)(a * b));

        [ExpressionOperation(ExpressionType.MultiplyChecked)]
        public static sbyte MultiplyChecked(sbyte a, sbyte b) => checked((sbyte)(a * b));

        [ExpressionOperation(ExpressionType.MultiplyChecked)]
        public static sbyte? MultiplyChecked(sbyte? a, sbyte? b) => checked((sbyte?)(a * b));

        [ExpressionOperation(ExpressionType.MultiplyChecked)]
        public static short MultiplyChecked(short a, short b) => checked((short)(a * b));

        [ExpressionOperation(ExpressionType.MultiplyChecked)]
        public static short? MultiplyChecked(short? a, short? b) => checked((short?)(a * b));

        [ExpressionOperation(ExpressionType.MultiplyChecked)]
        public static ushort MultiplyChecked(ushort a, ushort b) => checked((ushort)(a * b));

        [ExpressionOperation(ExpressionType.MultiplyChecked)]
        public static ushort? MultiplyChecked(ushort? a, ushort? b) => checked((ushort?)(a * b));

        [ExpressionOperation(ExpressionType.MultiplyChecked)]
        public static int MultiplyChecked(int a, int b) => checked(a * b);

        [ExpressionOperation(ExpressionType.MultiplyChecked)]
        public static int? MultiplyChecked(int? a, int? b) => checked(a * b);

        [ExpressionOperation(ExpressionType.MultiplyChecked)]
        public static uint MultiplyChecked(uint a, uint b) => checked(a * b);

        [ExpressionOperation(ExpressionType.MultiplyChecked)]
        public static uint? MultiplyChecked(uint? a, uint? b) => checked(a * b);

        [ExpressionOperation(ExpressionType.MultiplyChecked)]
        public static long MultiplyChecked(long a, long b) => checked(a * b);

        [ExpressionOperation(ExpressionType.MultiplyChecked)]
        public static long? MultiplyChecked(long? a, long? b) => checked(a * b);

        [ExpressionOperation(ExpressionType.MultiplyChecked)]
        public static ulong MultiplyChecked(ulong a, ulong b) => checked(a * b);

        [ExpressionOperation(ExpressionType.MultiplyChecked)]
        public static ulong? MultiplyChecked(ulong? a, ulong? b) => checked(a * b);

        #endregion MultiplyChecked

        #region Negate

        [ExpressionOperation(ExpressionType.Negate)]
        public static byte Negate(byte a) => (byte)(a * -1);

        [ExpressionOperation(ExpressionType.Negate)]
        public static byte? Negate(byte? a) => (byte?)(a * -1);

        [ExpressionOperation(ExpressionType.Negate)]
        public static sbyte Negate(sbyte a) => (sbyte)(a * -1);

        [ExpressionOperation(ExpressionType.Negate)]
        public static sbyte? Negate(sbyte? a) => (sbyte?)(a * -1);

        [ExpressionOperation(ExpressionType.Negate)]
        public static short Negate(short a) => (short)(a * -1);

        [ExpressionOperation(ExpressionType.Negate)]
        public static short? Negate(short? a) => (short?)(a * -1);

        [ExpressionOperation(ExpressionType.Negate)]
        public static ushort Negate(ushort a) => (ushort)(a * -1);

        [ExpressionOperation(ExpressionType.Negate)]
        public static ushort? Negate(ushort? a) => (ushort?)(a * -1);

        [ExpressionOperation(ExpressionType.Negate)]
        public static int Negate(int a) => a * -1;

        [ExpressionOperation(ExpressionType.Negate)]
        public static int? Negate(int? a) => a * -1;

        [ExpressionOperation(ExpressionType.Negate)]
        public static uint Negate(uint a) => (uint)(a * -1);

        [ExpressionOperation(ExpressionType.Negate)]
        public static uint? Negate(uint? a) => (uint?)(a * -1);

        [ExpressionOperation(ExpressionType.Negate)]
        public static long Negate(long a) => a * -1;

        [ExpressionOperation(ExpressionType.Negate)]
        public static long? Negate(long? a) => a * -1;

        [ExpressionOperation(ExpressionType.Negate)]
        public static ulong Negate(ulong a) => a * unchecked((ulong)-1);

        [ExpressionOperation(ExpressionType.Negate)]
        public static ulong? Negate(ulong? a) => a * unchecked((ulong)-1);

        [ExpressionOperation(ExpressionType.Negate)]
        public static float Negate(float a) => a * -1;

        [ExpressionOperation(ExpressionType.Negate)]
        public static float? Negate(float? a) => a * -1;

        [ExpressionOperation(ExpressionType.Negate)]
        public static double Negate(double a) => a * -1;

        [ExpressionOperation(ExpressionType.Negate)]
        public static double? Negate(double? a) => a * -1;

        [ExpressionOperation(ExpressionType.Negate)]
        public static decimal Negate(decimal a) => a * -1;

        [ExpressionOperation(ExpressionType.Negate)]
        public static decimal? Negate(decimal? a) => a * -1;

        #endregion Negate

        #region Not

        [ExpressionOperation(ExpressionType.Not)]
        public static bool Not(bool a) => !a;

        [ExpressionOperation(ExpressionType.Not)]
        public static bool? Not(bool? a) => !a;

        [ExpressionOperation(ExpressionType.Not)]
        public static byte Not(byte a) => (byte)~a;

        [ExpressionOperation(ExpressionType.Not)]
        public static byte? Not(byte? a) => (byte?)~a;

        [ExpressionOperation(ExpressionType.Not)]
        public static sbyte Not(sbyte a) => (sbyte)~a;

        [ExpressionOperation(ExpressionType.Not)]
        public static sbyte? Not(sbyte? a) => (sbyte?)~a;

        [ExpressionOperation(ExpressionType.Not)]
        public static short Not(short a) => (short)~a;

        [ExpressionOperation(ExpressionType.Not)]
        public static short? Not(short? a) => (short?)~a;

        [ExpressionOperation(ExpressionType.Not)]
        public static ushort Not(ushort a) => (ushort)~a;

        [ExpressionOperation(ExpressionType.Not)]
        public static ushort? Not(ushort? a) => (ushort?)~a;

        [ExpressionOperation(ExpressionType.Not)]
        public static int Not(int a) => ~a;

        [ExpressionOperation(ExpressionType.Not)]
        public static int? Not(int? a) => ~a;

        [ExpressionOperation(ExpressionType.Not)]
        public static uint Not(uint a) => ~a;

        [ExpressionOperation(ExpressionType.Not)]
        public static uint? Not(uint? a) => ~a;

        [ExpressionOperation(ExpressionType.Not)]
        public static long Not(long a) => ~a;

        [ExpressionOperation(ExpressionType.Not)]
        public static long? Not(long? a) => ~a;

        [ExpressionOperation(ExpressionType.Not)]
        public static ulong Not(ulong a) => ~a;

        [ExpressionOperation(ExpressionType.Not)]
        public static ulong? Not(ulong? a) => ~a;

        #endregion Not

        #region NotEqual

        [ExpressionOperation(ExpressionType.NotEqual)]
        public static bool NotEqual(byte a, byte b) => a != b;

        [ExpressionOperation(ExpressionType.NotEqual)]
        public static bool NotEqual(byte? a, byte? b) => a != b;

        [ExpressionOperation(ExpressionType.NotEqual)]
        public static bool NotEqual(sbyte a, sbyte b) => a != b;

        [ExpressionOperation(ExpressionType.NotEqual)]
        public static bool NotEqual(sbyte? a, sbyte? b) => a != b;

        [ExpressionOperation(ExpressionType.NotEqual)]
        public static bool NotEqual(short a, short b) => a != b;

        [ExpressionOperation(ExpressionType.NotEqual)]
        public static bool NotEqual(short? a, short? b) => a != b;

        [ExpressionOperation(ExpressionType.NotEqual)]
        public static bool NotEqual(int a, int b) => a != b;

        [ExpressionOperation(ExpressionType.NotEqual)]
        public static bool NotEqual(int? a, int? b) => a != b;

        [ExpressionOperation(ExpressionType.NotEqual)]
        public static bool NotEqual(long a, long b) => a != b;

        [ExpressionOperation(ExpressionType.NotEqual)]
        public static bool NotEqual(long? a, long? b) => a != b;

        [ExpressionOperation(ExpressionType.NotEqual)]
        public static bool NotEqual(float a, float b) => a != b;

        [ExpressionOperation(ExpressionType.NotEqual)]
        public static bool NotEqual(float? a, float? b) => a != b;

        [ExpressionOperation(ExpressionType.NotEqual)]
        public static bool NotEqual(double a, double b) => a != b;

        [ExpressionOperation(ExpressionType.NotEqual)]
        public static bool NotEqual(double? a, double? b) => a != b;

        [ExpressionOperation(ExpressionType.NotEqual)]
        public static bool NotEqual(decimal a, decimal b) => a != b;

        [ExpressionOperation(ExpressionType.NotEqual)]
        public static bool NotEqual(decimal? a, decimal? b) => a != b;

        [ExpressionOperation(ExpressionType.NotEqual)]
        public static bool NotEqual(DateTime a, DateTime b) => a != b;

        [ExpressionOperation(ExpressionType.NotEqual)]
        public static bool NotEqual(DateTime? a, DateTime? b) => a != b;

        [ExpressionOperation(ExpressionType.NotEqual)]
        public static bool NotEqual(TimeSpan a, TimeSpan b) => a != b;

        [ExpressionOperation(ExpressionType.NotEqual)]
        public static bool NotEqual(TimeSpan? a, TimeSpan? b) => a != b;

        [ExpressionOperation(ExpressionType.NotEqual)]
        public static bool NotEqual(object a, object b) => a != b;

        #endregion NotEqual

        #region OnesComplement

        [ExpressionOperation(ExpressionType.OnesComplement)]
        public static byte OnesComplement(byte a) => (byte)~a;

        [ExpressionOperation(ExpressionType.OnesComplement)]
        public static byte? OnesComplement(byte? a) => (byte?)~a;

        [ExpressionOperation(ExpressionType.OnesComplement)]
        public static sbyte OnesComplement(sbyte a) => (sbyte)~a;

        [ExpressionOperation(ExpressionType.OnesComplement)]
        public static sbyte? OnesComplement(sbyte? a) => (sbyte?)~a;

        [ExpressionOperation(ExpressionType.OnesComplement)]
        public static short OnesComplement(short a) => (short)~a;

        [ExpressionOperation(ExpressionType.OnesComplement)]
        public static short? OnesComplement(short? a) => (short?)~a;

        [ExpressionOperation(ExpressionType.OnesComplement)]
        public static ushort OnesComplement(ushort a) => (ushort)~a;

        [ExpressionOperation(ExpressionType.OnesComplement)]
        public static ushort? OnesComplement(ushort? a) => (ushort?)~a;

        [ExpressionOperation(ExpressionType.OnesComplement)]
        public static int OnesComplement(int a) => ~a;

        [ExpressionOperation(ExpressionType.OnesComplement)]
        public static int? OnesComplement(int? a) => ~a;

        [ExpressionOperation(ExpressionType.OnesComplement)]
        public static uint OnesComplement(uint a) => ~a;

        [ExpressionOperation(ExpressionType.OnesComplement)]
        public static uint? OnesComplement(uint? a) => ~a;

        [ExpressionOperation(ExpressionType.OnesComplement)]
        public static long OnesComplement(long a) => ~a;

        [ExpressionOperation(ExpressionType.OnesComplement)]
        public static long? OnesComplement(long? a) => ~a;

        [ExpressionOperation(ExpressionType.OnesComplement)]
        public static ulong OnesComplement(ulong a) => ~a;

        [ExpressionOperation(ExpressionType.OnesComplement)]
        public static ulong? OnesComplement(ulong? a) => ~a;

        #endregion OnesComplement

        #region Or

        [ExpressionOperation(ExpressionType.Or)]
        public static bool Or(bool a, bool b) => a | b;

        [ExpressionOperation(ExpressionType.Or)]
        public static byte Or(byte a, byte b) => (byte)(a | b);

        [ExpressionOperation(ExpressionType.Or)]
        public static byte? Or(byte? a, byte? b) => (byte?)(a | b);

        [ExpressionOperation(ExpressionType.Or)]
        public static sbyte Or(sbyte a, sbyte b) => (sbyte)(a | b);

        [ExpressionOperation(ExpressionType.Or)]
        public static sbyte? Or(sbyte? a, sbyte? b) => (sbyte?)(a | b);

        [ExpressionOperation(ExpressionType.Or)]
        public static short Or(short a, short b) => (short)(a | b);

        [ExpressionOperation(ExpressionType.Or)]
        public static short? Or(short? a, short? b) => (short?)(a | b);

        [ExpressionOperation(ExpressionType.Or)]
        public static ushort Or(ushort a, ushort b) => (ushort)(a | b);

        [ExpressionOperation(ExpressionType.Or)]
        public static ushort? Or(ushort? a, ushort? b) => (ushort?)(a | b);

        [ExpressionOperation(ExpressionType.Or)]
        public static int Or(int a, int b) => a | b;

        [ExpressionOperation(ExpressionType.Or)]
        public static int? Or(int? a, int? b) => a | b;

        [ExpressionOperation(ExpressionType.Or)]
        public static uint Or(uint a, uint b) => a | b;

        [ExpressionOperation(ExpressionType.Or)]
        public static uint? Or(uint? a, uint? b) => a | b;

        [ExpressionOperation(ExpressionType.Or)]
        public static long Or(long a, long b) => a | b;

        [ExpressionOperation(ExpressionType.Or)]
        public static long? Or(long? a, long? b) => a | b;

        [ExpressionOperation(ExpressionType.Or)]
        public static ulong Or(ulong a, ulong b) => a | b;

        [ExpressionOperation(ExpressionType.Or)]
        public static ulong? Or(ulong? a, ulong? b) => a | b;

        #endregion Or

        #region Power

        [ExpressionOperation(ExpressionType.Power)]
        public static byte Power(byte a, byte b) => (byte)Math.Pow(a, b);

        [ExpressionOperation(ExpressionType.Power)]
        public static byte? Power(byte? a, byte? b) => a == null || b == null ? null : (byte?)Math.Pow(a.Value, b.Value);

        [ExpressionOperation(ExpressionType.Power)]
        public static sbyte Power(sbyte a, sbyte b) => (sbyte)Math.Pow(a, b);

        [ExpressionOperation(ExpressionType.Power)]
        public static sbyte? Power(sbyte? a, sbyte? b) => a == null || b == null ? null : (sbyte?)Math.Pow(a.Value, b.Value);

        [ExpressionOperation(ExpressionType.Power)]
        public static short Power(short a, short b) => (short)Math.Pow(a, b);

        [ExpressionOperation(ExpressionType.Power)]
        public static short? Power(short? a, short? b) => a == null || b == null ? null : (short?)Math.Pow(a.Value, b.Value);

        [ExpressionOperation(ExpressionType.Power)]
        public static ushort Power(ushort a, ushort b) => (ushort)Math.Pow(a, b);

        [ExpressionOperation(ExpressionType.Power)]
        public static ushort? Power(ushort? a, ushort? b) => a == null || b == null ? null : (ushort?)Math.Pow(a.Value, b.Value);

        [ExpressionOperation(ExpressionType.Power)]
        public static int Power(int a, int b) => (int)Math.Pow(a, b);

        [ExpressionOperation(ExpressionType.Power)]
        public static int? Power(int? a, int? b) => a == null || b == null ? null : (int?)Math.Pow(a.Value, b.Value);

        [ExpressionOperation(ExpressionType.Power)]
        public static uint Power(uint a, uint b) => (uint)Math.Pow(a, b);

        [ExpressionOperation(ExpressionType.Power)]
        public static uint? Power(uint? a, uint? b) => a == null || b == null ? null : (uint?)Math.Pow(a.Value, b.Value);

        [ExpressionOperation(ExpressionType.Power)]
        public static long Power(long a, long b) => (long)Math.Pow(a, b);

        [ExpressionOperation(ExpressionType.Power)]
        public static long? Power(long? a, long? b) => a == null || b == null ? null : (long?)Math.Pow(a.Value, b.Value);

        [ExpressionOperation(ExpressionType.Power)]
        public static ulong Power(ulong a, ulong b) => (ulong)Math.Pow(a, b);

        [ExpressionOperation(ExpressionType.Power)]
        public static ulong? Power(ulong? a, ulong? b) => a == null || b == null ? null : (ulong?)Math.Pow(a.Value, b.Value);

        [ExpressionOperation(ExpressionType.Power)]
        public static float Power(float a, float b) => (float)Math.Pow(a, b);

        [ExpressionOperation(ExpressionType.Power)]
        public static float? Power(float? a, float? b) => a == null || b == null ? null : (float?)Math.Pow(a.Value, b.Value);

        [ExpressionOperation(ExpressionType.Power)]
        public static double Power(double a, double b) => Math.Pow(a, b);

        [ExpressionOperation(ExpressionType.Power)]
        public static double? Power(double? a, double? b) => a == null || b == null ? null : (double?)Math.Pow(a.Value, b.Value);

        [ExpressionOperation(ExpressionType.Power)]
        public static decimal Power(decimal a, decimal b) => (decimal)Math.Pow((double)a, (double)b);

        [ExpressionOperation(ExpressionType.Power)]
        public static decimal? Power(decimal? a, decimal? b) => a == null || b == null ? null : (decimal?)Math.Pow((double)a.Value, (double)b.Value);

        #endregion Power

        #region RightShift

        [ExpressionOperation(ExpressionType.RightShift)]
        public static byte RightShift(byte a, byte b) => (byte)(a >> b);

        [ExpressionOperation(ExpressionType.RightShift)]
        public static byte? RightShift(byte? a, byte? b) => (byte?)(a >> b);

        [ExpressionOperation(ExpressionType.RightShift)]
        public static sbyte RightShift(sbyte a, sbyte b) => (sbyte)(a >> b);

        [ExpressionOperation(ExpressionType.RightShift)]
        public static sbyte? RightShift(sbyte? a, sbyte? b) => (sbyte?)(a >> b);

        [ExpressionOperation(ExpressionType.RightShift)]
        public static short RightShift(short a, short b) => (short)(a >> b);

        [ExpressionOperation(ExpressionType.RightShift)]
        public static short? RightShift(short? a, short? b) => (short?)(a >> b);

        [ExpressionOperation(ExpressionType.RightShift)]
        public static ushort RightShift(ushort a, ushort b) => (ushort)(a >> b);

        [ExpressionOperation(ExpressionType.RightShift)]
        public static ushort? RightShift(ushort? a, ushort? b) => (ushort?)(a >> b);

        [ExpressionOperation(ExpressionType.RightShift)]
        public static int RightShift(int a, int b) => a >> b;

        [ExpressionOperation(ExpressionType.RightShift)]
        public static int? RightShift(int? a, int? b) => a >> b;

        [ExpressionOperation(ExpressionType.RightShift)]
        public static uint RightShift(uint a, int b) => a >> b;

        [ExpressionOperation(ExpressionType.RightShift)]
        public static uint? RightShift(uint? a, int? b) => a >> b;

        [ExpressionOperation(ExpressionType.RightShift)]
        public static long RightShift(long a, int b) => a >> b;

        [ExpressionOperation(ExpressionType.RightShift)]
        public static long? RightShift(long? a, int? b) => a >> b;

        [ExpressionOperation(ExpressionType.RightShift)]
        public static ulong RightShift(ulong a, int b) => a >> b;

        [ExpressionOperation(ExpressionType.RightShift)]
        public static ulong? RightShift(ulong? a, int? b) => a >> b;

        #endregion RightShift

        #region Subtract

        [ExpressionOperation(ExpressionType.Subtract)]
        public static byte Subtract(byte a, byte b) => (byte)(a - b);

        [ExpressionOperation(ExpressionType.Subtract)]
        public static byte? Subtract(byte? a, byte? b) => (byte?)(a - b);

        [ExpressionOperation(ExpressionType.Subtract)]
        public static sbyte Subtract(sbyte a, sbyte b) => (sbyte)(a - b);

        [ExpressionOperation(ExpressionType.Subtract)]
        public static sbyte? Subtract(sbyte? a, sbyte? b) => (sbyte?)(a - b);

        [ExpressionOperation(ExpressionType.Subtract)]
        public static short Subtract(short a, short b) => (short)(a - b);

        [ExpressionOperation(ExpressionType.Subtract)]
        public static short? Subtract(short? a, short? b) => (short?)(a - b);

        [ExpressionOperation(ExpressionType.Subtract)]
        public static ushort Subtract(ushort a, ushort b) => (ushort)(a - b);

        [ExpressionOperation(ExpressionType.Subtract)]
        public static ushort? Subtract(ushort? a, ushort? b) => (ushort?)(a - b);

        [ExpressionOperation(ExpressionType.Subtract)]
        public static int Subtract(int a, int b) => a - b;

        [ExpressionOperation(ExpressionType.Subtract)]
        public static int? Subtract(int? a, int? b) => a - b;

        [ExpressionOperation(ExpressionType.Subtract)]
        public static uint Subtract(uint a, uint b) => a - b;

        [ExpressionOperation(ExpressionType.Subtract)]
        public static uint? Subtract(uint? a, uint? b) => a - b;

        [ExpressionOperation(ExpressionType.Subtract)]
        public static long Subtract(long a, long b) => a - b;

        [ExpressionOperation(ExpressionType.Subtract)]
        public static long? Subtract(long? a, long? b) => a - b;

        [ExpressionOperation(ExpressionType.Subtract)]
        public static ulong Subtract(ulong a, ulong b) => a - b;

        [ExpressionOperation(ExpressionType.Subtract)]
        public static ulong? Subtract(ulong? a, ulong? b) => a - b;

        [ExpressionOperation(ExpressionType.Subtract)]
        public static float Subtract(float a, float b) => a - b;

        [ExpressionOperation(ExpressionType.Subtract)]
        public static float? Subtract(float? a, float? b) => a - b;

        [ExpressionOperation(ExpressionType.Subtract)]
        public static double Subtract(double a, double b) => a - b;

        [ExpressionOperation(ExpressionType.Subtract)]
        public static double? Subtract(double? a, double? b) => a - b;

        [ExpressionOperation(ExpressionType.Subtract)]
        public static decimal Subtract(decimal a, decimal b) => a - b;

        [ExpressionOperation(ExpressionType.Subtract)]
        public static decimal? Subtract(decimal? a, decimal? b) => a - b;

        [ExpressionOperation(ExpressionType.Subtract)]
        public static DateTime Subtract(DateTime a, TimeSpan b) => a - b;

        [ExpressionOperation(ExpressionType.Subtract)]
        public static DateTime? Subtract(DateTime? a, TimeSpan? b) => a - b;

        [ExpressionOperation(ExpressionType.Subtract)]
        public static TimeSpan Subtract(TimeSpan a, TimeSpan b) => a - b;

        [ExpressionOperation(ExpressionType.Subtract)]
        public static TimeSpan? Subtract(TimeSpan? a, TimeSpan? b) => a - b;

        #endregion Subtract

        #region SubtractChecked

        [ExpressionOperation(ExpressionType.SubtractChecked)]
        public static byte SubtractChecked(byte a, byte b) => checked((byte)(a - b));

        [ExpressionOperation(ExpressionType.SubtractChecked)]
        public static byte? SubtractChecked(byte? a, byte? b) => checked((byte?)(a - b));

        [ExpressionOperation(ExpressionType.SubtractChecked)]
        public static sbyte SubtractChecked(sbyte a, sbyte b) => checked((sbyte)(a - b));

        [ExpressionOperation(ExpressionType.SubtractChecked)]
        public static sbyte? SubtractChecked(sbyte? a, sbyte? b) => checked((sbyte?)(a - b));

        [ExpressionOperation(ExpressionType.SubtractChecked)]
        public static short SubtractChecked(short a, short b) => checked((short)(a - b));

        [ExpressionOperation(ExpressionType.SubtractChecked)]
        public static short? SubtractChecked(short? a, short? b) => checked((short?)(a - b));

        [ExpressionOperation(ExpressionType.SubtractChecked)]
        public static ushort SubtractChecked(ushort a, ushort b) => checked((ushort)(a - b));

        [ExpressionOperation(ExpressionType.SubtractChecked)]
        public static ushort? SubtractChecked(ushort? a, ushort? b) => checked((ushort?)(a - b));

        [ExpressionOperation(ExpressionType.SubtractChecked)]
        public static int SubtractChecked(int a, int b) => checked(a - b);

        [ExpressionOperation(ExpressionType.SubtractChecked)]
        public static int? SubtractChecked(int? a, int? b) => checked(a - b);

        [ExpressionOperation(ExpressionType.SubtractChecked)]
        public static uint SubtractChecked(uint a, uint b) => checked(a - b);

        [ExpressionOperation(ExpressionType.SubtractChecked)]
        public static uint? SubtractChecked(uint? a, uint? b) => checked(a - b);

        [ExpressionOperation(ExpressionType.SubtractChecked)]
        public static long SubtractChecked(long a, long b) => checked(a - b);

        [ExpressionOperation(ExpressionType.SubtractChecked)]
        public static long? SubtractChecked(long? a, long? b) => checked(a - b);

        [ExpressionOperation(ExpressionType.SubtractChecked)]
        public static ulong SubtractChecked(ulong a, ulong b) => checked(a - b);

        [ExpressionOperation(ExpressionType.SubtractChecked)]
        public static ulong? SubtractChecked(ulong? a, ulong? b) => checked(a - b);

        #endregion SubtractChecked

        #region UnaryPlus

        [ExpressionOperation(ExpressionType.UnaryPlus)]
        public static byte UnaryPlus(byte a) => a;

        [ExpressionOperation(ExpressionType.UnaryPlus)]
        public static byte? UnaryPlus(byte? a) => a;

        [ExpressionOperation(ExpressionType.UnaryPlus)]
        public static sbyte UnaryPlus(sbyte a) => a;

        [ExpressionOperation(ExpressionType.UnaryPlus)]
        public static sbyte? UnaryPlus(sbyte? a) => a;

        [ExpressionOperation(ExpressionType.UnaryPlus)]
        public static short UnaryPlus(short a) => a;

        [ExpressionOperation(ExpressionType.UnaryPlus)]
        public static short? UnaryPlus(short? a) => a;

        [ExpressionOperation(ExpressionType.UnaryPlus)]
        public static ushort UnaryPlus(ushort a) => a;

        [ExpressionOperation(ExpressionType.UnaryPlus)]
        public static ushort? UnaryPlus(ushort? a) => a;

        [ExpressionOperation(ExpressionType.UnaryPlus)]
        public static int UnaryPlus(int a) => a;

        [ExpressionOperation(ExpressionType.UnaryPlus)]
        public static int? UnaryPlus(int? a) => a;

        [ExpressionOperation(ExpressionType.UnaryPlus)]
        public static uint UnaryPlus(uint a) => a;

        [ExpressionOperation(ExpressionType.UnaryPlus)]
        public static uint? UnaryPlus(uint? a) => a;

        [ExpressionOperation(ExpressionType.UnaryPlus)]
        public static long UnaryPlus(long a) => a;

        [ExpressionOperation(ExpressionType.UnaryPlus)]
        public static long? UnaryPlus(long? a) => a;

        [ExpressionOperation(ExpressionType.UnaryPlus)]
        public static ulong UnaryPlus(ulong a) => a;

        [ExpressionOperation(ExpressionType.UnaryPlus)]
        public static ulong? UnaryPlus(ulong? a) => a;

        [ExpressionOperation(ExpressionType.UnaryPlus)]
        public static float UnaryPlus(float a) => a;

        [ExpressionOperation(ExpressionType.UnaryPlus)]
        public static float? UnaryPlus(float? a) => a;

        [ExpressionOperation(ExpressionType.UnaryPlus)]
        public static double UnaryPlus(double a) => a;

        [ExpressionOperation(ExpressionType.UnaryPlus)]
        public static double? UnaryPlus(double? a) => a;

        [ExpressionOperation(ExpressionType.UnaryPlus)]
        public static decimal UnaryPlus(decimal a) => a;

        [ExpressionOperation(ExpressionType.UnaryPlus)]
        public static decimal? UnaryPlus(decimal? a) => a;

        #endregion UnaryPlus
    }
}
