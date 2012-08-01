using System;
using System.IO;
using System.Linq.Expressions;

namespace EventEvaluationLib
{
    public static class StreamReaderExtensions
    {
        static Func<StreamReader, int> charLenAcc = GetFieldAccessor<StreamReader, int>("charLen");
        static Func<StreamReader, int> charPosAcc = GetFieldAccessor<StreamReader, int>("charPos");

        public static Func<T, R> GetFieldAccessor<T, R>(string fieldName)
        {
            ParameterExpression param =
            Expression.Parameter(typeof(T), "arg");

            MemberExpression member =
            Expression.Field(param, fieldName);


            LambdaExpression lambda =
            Expression.Lambda(typeof(Func<T, R>), member, param);

            Func<T, R> compiled = (Func<T, R>)lambda.Compile();
            return compiled;
        }


        public static long GetRealPosition(this StreamReader sr)
        {
            return sr.BaseStream.Position - charLenAcc(sr) + charPosAcc(sr);
        }


    }
}