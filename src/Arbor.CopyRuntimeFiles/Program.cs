using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Arbor.CopyRuntimeFiles
{
    [UsedImplicitly]
    internal class Program
    {
        private static Task<int> Main(string[] args)
        {
            return App.CreateAndRunAsync(args);
        }
    }
}
