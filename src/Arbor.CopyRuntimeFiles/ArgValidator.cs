using System;

namespace Arbor.CopyRuntimeFiles
{
    internal class ArgValidator
    {
        public int Validate(string[] args)
        {
            Console.WriteLine("First argument: source project, relative to repository root, example: 'src\\SourceProject\\");
            Console.WriteLine("Second argument: target project, relative to repository root, example: 'src\\TargetProject\\");
            Console.WriteLine("Third argument: filters, semicolon separated list of extensions, example: *.cshtml;*.pdf");
            Console.WriteLine("Fourth argument: optional black-listed directory names, semicolon separated, example: bin;obj;node_modules");

            if (args.Length == 0)
            {
                Console.WriteLine("Missing first argument source project, relative to repository root");
                return 1;
            }

            if (args.Length <= 1)
            {
                Console.WriteLine("Missing second argument target project, relative to repository root");
                return 1;
            }

            if (args.Length <= 2)
            {
                Console.WriteLine("Missing third argument filter, semicolon separated list of extensions, example: *.cshtml;*.pdf");
                return 1;
            }

            return 0;
        }
    }
}