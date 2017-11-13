using System.Threading.Tasks;

namespace Arbor.CopyRuntimeFiles
{
    internal class Program
    {
        private static Task<int> Main(string[] args)
        {
            return App.CreateAndRunAsync(args);
        }
    }
}