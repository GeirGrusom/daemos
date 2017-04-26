using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Markurion.Mute
{
    using Interperator;
    using Scripting;
    public sealed class MuteScriptRunner : IScriptRunner
    {
        public Action<IDependencyResolver> Compile(string code)
        {
            var compiler = new Compiler();
            var result = compiler.Compile(code);

            /*            return () =>
                        {

                        }*/

            throw new NotImplementedException();
        }

        public void Run(string code, IDependencyResolver resolver)
        {
            throw new NotImplementedException();
        }
    }
}
