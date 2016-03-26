using Dashji;
using System;

namespace Mood
{
    public class Arguments
    {
        public Arguments()
        {

        }

        public bool Save { get; set; }
        public bool Load { get; set; }
        public bool Delete { get; set; }
        public bool Verbose { get; set; }
        public bool List { get; set; }
        public bool DontKill { get; set; }
        public bool Restart { get; set; }

        public string FilePath { get; set; }
    }

    class Program
    {
        static TaskbarManager tbm = new TaskbarManager();

        static void Main(string[] _args)
        {
#if DEBUG
            //var fprs = new FluentParser<Arguments>("-s default".Split(' '));
            var fprs = new FluentParser<Arguments>("-l default".Split(' '));
#else
            var fprs = new FluentParser<Arguments>(_args);
#endif

            fprs.Define(x => x.Load)
                .Long("load")
                .Short('l');

            fprs.Define(x => x.Save)
                .Long("save")
                .Short('s');

            fprs.Define(x => x.Delete)
                .Long("delete")
                .Short('d');

            fprs.Define(x => x.Verbose)
                .Long("verbose")
                .Short('v');

            fprs.Define(x => x.List)
                .Long("list", "all")
                .Short('a');

            fprs.Define(x => x.DontKill)
                .Long("no-kill")
                .Short('n');

            fprs.Define(x => x.Restart)
                .Long("restart")
                .Short('r');

            fprs.Define(x => x.FilePath)
                .Main();

            Arguments args = fprs.Item;

            if (args.Save)
            {
                tbm.SaveTo(args.FilePath, args.Verbose);
            }
            else if (args.List)
            {
                tbm.List();
            }
            else if (args.Delete)
            {
                tbm.Remove(args.FilePath, args.Verbose);
            }
            else if (args.FilePath != null)
            {
                tbm.LoadFrom(args.FilePath, args.Verbose, !args.DontKill, args.Restart);
            }
            else 
            {
                // show help
                Console.WriteLine("mode [options] file");
                Console.WriteLine();
                Console.WriteLine("    -l  --load     Load to taskbar");
                Console.WriteLine("    -s  --save     Save from current taskbar");
                Console.WriteLine("    -d  --delete   Delete save");
                Console.WriteLine("    -v  --verbose  Print informations");
                Console.WriteLine("    -a  --all      List saves");
                Console.WriteLine("    -n  --no-kill  Don't kill explorer.exe");
                Console.WriteLine("    -r  --restart  Restart explorer.exe");
            }

#if DEBUG
            Console.ReadKey();
#endif
        }
    }
}
