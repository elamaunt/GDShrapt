using System.IO;

namespace GDScriptConverter
{
    public class GDScriptParser
    {
        //public GDProject Project { get; } = new GDProject();

        public GDTypeDeclaration ParseFileContent(string content)
        {
            var state = new GDReadingState();

            state.ContentStarted();

            using (var reader = new StringReader(content))
            {
                string line;
                while((line = reader.ReadLine()) != null)
                    ParseLine(line, state);
            }

            state.ContentFinished();

            return state.Type;
        }

        public GDTypeDeclaration ParseFile(string filePath)
        {
            var state = new GDReadingState();

            state.ContentStarted();

            foreach (var line in File.ReadLines(filePath))
                ParseLine(line, state);

            state.ContentFinished();

            return state.Type;
        }

        private void ParseLine(string line, GDReadingState state)
        {
            state.LineStarted();

            for (int i = 0; i < line.Length; i++)
                state.HandleChar(line[i]);

            state.LineFinished();
        }
    }
}