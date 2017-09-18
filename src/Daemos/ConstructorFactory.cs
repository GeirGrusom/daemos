using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Daemos
{
    public class ConstructorFactory
    {
        public Func<IDependencyResolver, T> Create<T>()
             where T : class
        {
            var t = typeof(T);
            var constructors = t.GetConstructors();
            var ctor = constructors.Where(x => x.IsPublic).OrderByDescending(x => x.GetParameters().Length).FirstOrDefault();

            if (ctor == null)
            {
                throw new InvalidOperationException("Type does not have any public constructors");
            }

            var dependencyResolverArgument = Expression.Parameter(typeof(IDependencyResolver), "dependencyResolver");

            var parameters = ctor.GetParameters();
            if (parameters.Count() == 0)
            {
                var lambda = Expression.Lambda<Func<IDependencyResolver, T>>(Expression.New(ctor), dependencyResolverArgument);
                return lambda.Compile();
            }

            var arguments = new Expression[parameters.Length];
            var resolveMethod = typeof(IDependencyResolver).GetMethod("GetService", new Type[1] { typeof(string) });


            for(int i = 0; i < parameters.Length; ++i)
            {
                if (parameters[i].ParameterType.GetTypeInfo().IsClass || parameters[i].ParameterType.GetTypeInfo().IsInterface)
                {
                    var resolveMethodType = resolveMethod.MakeGenericMethod(parameters[i].ParameterType);
                    var argVar = Expression.Variable(parameters[i].ParameterType);
                    arguments[i] = Expression.Block(parameters[i].ParameterType, new[] { argVar },
                        Expression.Assign(argVar, Expression.Call(dependencyResolverArgument, resolveMethodType, Expression.Constant(null, typeof(string)))),
                        Expression.IfThen(Expression.Equal(argVar, Expression.Constant(null, parameters[i].ParameterType)), Expression.Throw(Expression.New(typeof(DependencyFailedException).GetConstructor(new[] { typeof(Type) }), Expression.Constant(parameters[i].ParameterType)))),
                        argVar
                        );
                }
                else
                {
                    if(!parameters[i].HasDefaultValue)
                    {
                        throw new DependencyFailedException(parameters[i].ParameterType);
                    }
                    arguments[i] = Expression.Constant(parameters[i].DefaultValue);
                }
            }

            var constructorLambda = Expression.Lambda<Func<IDependencyResolver, T>>(Expression.New(ctor, arguments), dependencyResolverArgument);
            return constructorLambda.Compile();
        }
    }
}
