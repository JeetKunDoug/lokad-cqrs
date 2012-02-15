﻿using System.CodeDom.Compiler;
using System.IO;
using System.Text;

namespace Dsl
{
    public static class GeneratorUtil
    {
        public static string Build(string source, IGenerateCode generator)
        {
            return Build(GenerateContext(source), generator);
        }

        public static string Build(Context context, IGenerateCode generator)
        {
            var builder = new StringBuilder();
            using (var stream = new StringWriter(builder))
            using (var writer = new IndentedTextWriter(stream, "    "))
            {
                generator.Generate(context, writer);
            }
            return builder.ToString();
        }

        public static Context GenerateContext(string source)
        {
            var context = new MessageContractAssembler().From(source);
            return context;
        }

        public static string ParameterCase(string s)
        {
            return char.ToLowerInvariant(s[0]) + s.Substring(1);
        }

        public static string MemberCase(string s)
        {
            return char.ToUpperInvariant(s[0]) + s.Substring(1);
        }

        //public static string Extend(this GeneratedTextTransformation )
    }
}