// Signature-only stand-ins for NUnit and Unity's test framework, matching the
// `nunit.framework.dll` precompiled reference and the UnityEngine.TestRunner /
// UnityEditor.TestRunner assemblies the test asmdefs name.

using System;
using System.Collections;
using System.Collections.Generic;

namespace NUnit.Framework
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class TestFixtureAttribute : Attribute
    {
        public TestFixtureAttribute() { }
        public TestFixtureAttribute(params object[] arguments) { }
        public string Description { get; set; }
        public string Category { get; set; }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class TestAttribute : Attribute
    {
        public string Description { get; set; }
        public object ExpectedResult { get; set; }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class TestCaseAttribute : Attribute
    {
        public TestCaseAttribute(params object[] arguments) { }
        public object ExpectedResult { get; set; }
        public string TestName { get; set; }
        public string Description { get; set; }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class SetUpAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class TearDownAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class OneTimeSetUpAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class OneTimeTearDownAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class CategoryAttribute : Attribute
    {
        public CategoryAttribute(string name) { }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public class DescriptionAttribute : Attribute
    {
        public DescriptionAttribute(string description) { }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class IgnoreAttribute : Attribute
    {
        public IgnoreAttribute(string reason) { }
    }

    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    public class ValuesAttribute : Attribute
    {
        public ValuesAttribute(params object[] args) { }
    }

    public class AssertionException : Exception
    {
        public AssertionException(string message) : base(message) { }
    }

    public class SuccessException : Exception
    {
        public SuccessException(string message) : base(message) { }
    }

    public class IgnoreException : Exception
    {
        public IgnoreException(string message) : base(message) { }
    }

    public abstract class Constraint { }

    public static class Is
    {
        public static Constraint True { get { return null; } }
        public static Constraint False { get { return null; } }
        public static Constraint Null { get { return null; } }
        public static Constraint NotNull { get { return null; } }
        public static Constraint Empty { get { return null; } }
        public static Constraint Positive { get { return null; } }
        public static Constraint Negative { get { return null; } }
        public static Constraint Zero { get { return null; } }
        public static Constraint EqualTo(object expected) { return null; }
        public static Constraint GreaterThan(object expected) { return null; }
        public static Constraint GreaterThanOrEqualTo(object expected) { return null; }
        public static Constraint LessThan(object expected) { return null; }
        public static Constraint LessThanOrEqualTo(object expected) { return null; }
        public static Constraint InstanceOf<T>() { return null; }
    }

    public static class Assert
    {
        public static void AreEqual(object expected, object actual) { }
        public static void AreEqual(object expected, object actual, string message, params object[] args) { }
        public static void AreEqual(double expected, double actual, double delta) { }
        public static void AreEqual(double expected, double actual, double delta, string message, params object[] args) { }
        public static void AreNotEqual(object expected, object actual) { }
        public static void AreNotEqual(object expected, object actual, string message, params object[] args) { }
        public static void AreSame(object expected, object actual) { }
        public static void AreSame(object expected, object actual, string message, params object[] args) { }
        public static void AreNotSame(object expected, object actual) { }
        public static void AreNotSame(object expected, object actual, string message, params object[] args) { }

        public static void IsTrue(bool condition) { }
        public static void IsTrue(bool condition, string message, params object[] args) { }
        public static void IsFalse(bool condition) { }
        public static void IsFalse(bool condition, string message, params object[] args) { }
        public static void IsNull(object anObject) { }
        public static void IsNull(object anObject, string message, params object[] args) { }
        public static void IsNotNull(object anObject) { }
        public static void IsNotNull(object anObject, string message, params object[] args) { }
        public static void IsEmpty(IEnumerable collection) { }
        public static void IsNotEmpty(IEnumerable collection) { }

        public static void Greater(double arg1, double arg2) { }
        public static void Greater(double arg1, double arg2, string message, params object[] args) { }
        public static void Greater(int arg1, int arg2) { }
        public static void Greater(int arg1, int arg2, string message, params object[] args) { }
        public static void GreaterOrEqual(double arg1, double arg2) { }
        public static void GreaterOrEqual(double arg1, double arg2, string message, params object[] args) { }
        public static void GreaterOrEqual(int arg1, int arg2) { }
        public static void GreaterOrEqual(int arg1, int arg2, string message, params object[] args) { }
        public static void Less(double arg1, double arg2) { }
        public static void Less(double arg1, double arg2, string message, params object[] args) { }
        public static void Less(int arg1, int arg2) { }
        public static void Less(int arg1, int arg2, string message, params object[] args) { }
        public static void LessOrEqual(double arg1, double arg2) { }
        public static void LessOrEqual(double arg1, double arg2, string message, params object[] args) { }
        public static void LessOrEqual(int arg1, int arg2) { }
        public static void LessOrEqual(int arg1, int arg2, string message, params object[] args) { }

        public static void That(bool condition) { }
        public static void That(bool condition, string message, params object[] args) { }
        public static void That(object actual, Constraint expression) { }
        public static void That(object actual, Constraint expression, string message, params object[] args) { }

        public static void Fail() { }
        public static void Fail(string message, params object[] args) { }
        public static void Pass() { }
        public static void Pass(string message, params object[] args) { }
        public static void Ignore() { }
        public static void Ignore(string message, params object[] args) { }
        public static void Inconclusive(string message, params object[] args) { }
    }

    public static class CollectionAssert
    {
        public static void AreEqual(IEnumerable expected, IEnumerable actual) { }
        public static void Contains(IEnumerable collection, object actual) { }
        public static void IsEmpty(IEnumerable collection) { }
        public static void IsNotEmpty(IEnumerable collection) { }
        public static void AllItemsAreUnique(IEnumerable collection) { }
    }

    public static class StringAssert
    {
        public static void Contains(string expected, string actual) { }
        public static void StartsWith(string expected, string actual) { }
        public static void EndsWith(string expected, string actual) { }
        public static void AreEqualIgnoringCase(string expected, string actual) { }
    }
}

namespace UnityEngine.TestTools
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class UnityTestAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class UnitySetUpAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class UnityTearDownAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
    public class UnityPlatformAttribute : Attribute
    {
        public UnityPlatformAttribute(params RuntimePlatform[] include) { }
        public RuntimePlatform[] include { get; set; }
        public RuntimePlatform[] exclude { get; set; }
    }

    public static class LogAssert
    {
        public static bool ignoreFailingMessages { get; set; }
        public static void Expect(LogType type, string message) { }
        public static void Expect(LogType type, System.Text.RegularExpressions.Regex message) { }
        public static void NoUnexpectedReceived() { }
    }

    public class MonoBehaviourTest<T> : CustomYieldInstruction where T : MonoBehaviour, IMonoBehaviourTest
    {
        public T component { get { return null; } }
        public GameObject gameObject { get { return null; } }
        public override bool keepWaiting { get { return false; } }
    }

    public interface IMonoBehaviourTest { bool IsTestFinished { get; } }

    public static class TestTools
    {
    }
}

namespace UnityEngine.TestTools.Utils
{
    public class FloatEqualityComparer : IEqualityComparer<float>
    {
        public FloatEqualityComparer() { }
        public FloatEqualityComparer(float allowedError) { }
        public bool Equals(float expected, float actual) { return false; }
        public int GetHashCode(float value) { return 0; }
        public static FloatEqualityComparer Instance { get { return null; } }
    }

    public class Vector3EqualityComparer : IEqualityComparer<Vector3>
    {
        public Vector3EqualityComparer() { }
        public Vector3EqualityComparer(float allowedError) { }
        public bool Equals(Vector3 expected, Vector3 actual) { return false; }
        public int GetHashCode(Vector3 value) { return 0; }
        public static Vector3EqualityComparer Instance { get { return null; } }
    }
}
