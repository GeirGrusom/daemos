using System;
using System.Linq;
using System.Linq.Expressions;
using static System.Linq.Expressions.Expression;
using System.Reflection;
using System.Collections.Generic;
using System.Text;

namespace Markurion
{
    public class ConstructorFactory
    {
        public Func<IDependencyResolver, T> Create<T>()
             where T : class
        {
            var t = typeof(T);
            var constructors = t.GetConstructors();
            var ctor = constructors.Where(x => x.IsPublic).OrderByDescending(x => x.GetParameters().Length).FirstOrDefault();

            if(ctor == null)
            {
                throw new InvalidOperationException("Type does not have any public constructors");
            }

            var dependencyResolverArgument = Expression.Parameter(typeof(IDependencyResolver), "dependencyResolver");

            var parameters = ctor.GetParameters();
            if(parameters.Count() == 0)
            {
                var lambda = Lambda<Func<IDependencyResolver, T>>(New(ctor), dependencyResolverArgument);
                return lambda.Compile();
            }

            var arguments = new Expression[parameters.Length];
            var resolveMethod = typeof(IDependencyResolver).GetMethod("GetService", new Type[0]);


            for(int i = 0; i < parameters.Length; ++i)
            {
                if (parameters[i].ParameterType.GetTypeInfo().IsClass || parameters[i].ParameterType.GetTypeInfo().IsInterface)
                {
                    var resolveMethodType = resolveMethod.MakeGenericMethod(parameters[i].ParameterType);
                    var argVar = Variable(parameters[i].ParameterType);
                    arguments[i] = Block(parameters[i].ParameterType, new[] { argVar },
                        Assign(argVar, Call(dependencyResolverArgument, resolveMethodType)),
                        IfThen(Equal(argVar, Constant(null, parameters[i].ParameterType)), Throw(New(typeof(DependencyFailedException).GetConstructor(new[] { typeof(Type) }), Constant(parameters[i].ParameterType)))),
                        argVar
                        );
                }
                else
                {
                    if(!parameters[i].HasDefaultValue)
                    {
                        throw new DependencyFailedException(parameters[i].ParameterType);
                    }
                    arguments[i] = Constant(parameters[i].DefaultValue);
                }
            }

            var constructorLambda = Lambda<Func<IDependencyResolver, T>>(New(ctor, arguments), dependencyResolverArgument);
            return constructorLambda.Compile();
        }
    }
}
