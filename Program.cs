using JsonFx.Json;
using System.IO;

namespace Preprocessor
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            if(args.Length != 3 && args.Length != 1)
            {
                System.Console.WriteLine("Usage: Parser.exe <client_map> <api_output_path> <unit_tests_output_path>");
                System.Console.ReadLine();
                return;
            }

            if(args.Length == 1)
            {
                args = new string[] { args[0], "Generated", "Tests" };
            }

            System.Console.WriteLine("Loading client map");
            ClientMapping src = JsonReader.Deserialize<ClientMapping>(File.ReadAllText(args[0]));
            System.Console.WriteLine("Client map loaded");

            Generator.SetClientMap(src);
            Generator.AddAdditionalMapping(JsonReader.Deserialize<ClientMapping>(File.ReadAllText("enums.json")));
            Generator.LoadTemplate("Class_Template.cs");
            Generator.LoadUTTemplate("UT_Template.cs");
            Generator.LoadEnumTeplate("Enum_Template.cs");

            System.Console.WriteLine("Templates loaded");

            var api_files = Generator.Generate();

            Utils.Empty(args[1]);

            foreach (var item in api_files)
            {
                File.WriteAllText(Path.Combine(args[1], item.Key), Utils.Beautify(item.Value));
            }

            System.Console.WriteLine("API Files generated and saved");

            var ut_files = Generator.GenerateUnitTests();

            Utils.Empty(args[2]);

            foreach (var item in ut_files)
            {
                File.WriteAllText(Path.Combine(args[2], item.Key), Utils.Beautify(item.Value));
            }

            System.Console.WriteLine("Unit tests generated and saved");
        }
    }
}