using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Oku.Utils
{
    /// <summary>
    /// A basic wrapper for Oku internal logging... lets Oku do some work under the hood if needed.
    /// </summary>
    public static class OkuLog
    {
        private const string Prefix = "Oku";

        private const string DateFormat = "yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'ffffff";

        private static string InternalFormat(string str)
            => $"[{DateTime.Now.ToString(DateFormat)}][{Prefix}] {str}";

        /// <inheritdoc cref="Debug.Log"/>
        public static void Info(string message)
        {
            Debug.Log(InternalFormat(message));
        }
        /// <inheritdoc cref="Debug.LogFormat"/>
        public static void Info(string format, params object[] args)
        {
            Debug.LogFormat(InternalFormat(format), args);
        }

        /// <inheritdoc cref="Debug.LogWarning"/>
        public static void Warn(string message)
        {
            Debug.LogWarning(InternalFormat(message));
        }
        /// <inheritdoc cref="Debug.LogWarningFormat"/>
        public static void Warn(string format, params object[] args)
        {
            Debug.LogWarningFormat(InternalFormat(format), args);
        }

        /// <inheritdoc cref="Debug.LogError"/>
        public static void Error(string message)
        {
            Debug.LogError(InternalFormat(message));
        }
        /// <inheritdoc cref="Debug.LogErrorFormat"/>
        public static void Error(string format, params object[] args)
        {
            Debug.LogErrorFormat(InternalFormat(format), args);
        }

        /// <inheritdoc cref="Debug.Assert"/>
        public static void Assert(bool condition, string message)
        {
            Debug.Assert(condition, InternalFormat(message));
        }

        public static void Assert(bool condition, string message, Action executeOnFailure)
        {
            Assert(condition, message);
            if (!condition) executeOnFailure.Invoke();
        }
        /// <inheritdoc cref="Debug.AssertFormat"/>
        public static void Assert(bool condition, string format, params object[] args)
        {
            Debug.AssertFormat(condition, InternalFormat(format), args);
        }
    }
}
