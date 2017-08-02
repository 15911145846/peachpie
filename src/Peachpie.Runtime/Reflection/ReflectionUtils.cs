﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Pchp.Core.Dynamic;

namespace Pchp.Core.Reflection
{
    public static class ReflectionUtils
    {
        /// <summary>
        /// Well-known name of the PHP constructor.
        /// </summary>
        public const string PhpConstructorName = "__construct";

        /// <summary>
        /// Well-known name of the PHP destructor.
        /// </summary>
        public const string PhpDestructorName = "__destruct";

        readonly static char[] _disallowedNameChars = new char[] { '`', '<', '>', '.', '\'', '"', '#', '!' };

        /// <summary>
        /// Determines whether given name is valid PHP field, function or class name.
        /// </summary>
        public static bool IsAllowedPhpName(string name) => name != null && name.IndexOfAny(_disallowedNameChars) < 0;

        /// <summary>
        /// Checks the fields represents special PHP runtime fields.
        /// </summary>
        public static bool IsRuntimeFields(FieldInfo fld)
        {
            // TODO: [CompilerGenerated] attribute
            // internal PhpArray <runtime_fields>;
            return
                (fld.Name == "__peach__runtimeFields" || fld.Name == "<runtime_fields>") &&
                !fld.IsPublic && !fld.IsStatic && fld.FieldType == typeof(PhpArray);
        }

        /// <summary>
        /// Checks if the field represents special PHP context holder.
        /// </summary>
        public static bool IsContextField(FieldInfo fld)
        {
            // TODO: [CompilerGenerated] attribute
            // protected Context _ctx|<ctx>;
            return !fld.IsStatic &&
                (fld.Attributes & FieldAttributes.Family) != 0 &&
                (fld.Name == "_ctx" || fld.Name == "<ctx>") &&
                fld.FieldType == typeof(Context);
        }

        /// <summary>
        /// Determines whether given constructor is <c>PhpFieldsOnlyCtorAttribute</c>.
        /// </summary>
        public static bool IsPhpFieldsOnlyCtor(this ConstructorInfo ctor)
        {
            return ctor.IsFamilyOrAssembly && !ctor.IsStatic && ctor.GetCustomAttribute<PhpFieldsOnlyCtorAttribute>() != null;
        }

        /// <summary>
        /// Gets value indicating the given type is a type of a class instance excluding builtin PHP value types.
        /// </summary>
        public static bool IsClassType(TypeInfo tinfo)
        {
            Debug.Assert(tinfo != null);
            Debug.Assert(tinfo.AsType() != typeof(PhpAlias));

            var t = tinfo.AsType();
            return !tinfo.IsValueType && t != typeof(PhpArray) && t != typeof(string) && t != typeof(PhpString);
        }

        readonly static HashSet<Type> s_hiddenTypes = new HashSet<Type>()
        {
            typeof(object),
            typeof(IPhpCallable),
            typeof(PhpResource),
            typeof(System.Exception),
            typeof(System.Dynamic.IDynamicMetaObjectProvider),
        };

        /// <summary>
        /// Determines if given type is not visible to PHP runtime.
        /// We implement these types implicitly in compile time, so we should ignore them at proper places.
        /// </summary>
        public static bool IsHiddenType(this Type t) => s_hiddenTypes.Contains(t);

        /// <summary>
        /// Determines the parameter is considered as implicitly passed by runtime.
        /// </summary>
        public static bool IsImplicitParameter(ParameterInfo p) => BinderHelpers.IsImplicitParameter(p);

        /// <summary>
        /// Gets count of implicit parameters.
        /// Such parameters are passed by runtime automatically and not read from given arguments.
        /// </summary>
        public static int ImplicitParametersCount(ParameterInfo[] ps) => ps.TakeWhile(IsImplicitParameter).Count();
    }
}
