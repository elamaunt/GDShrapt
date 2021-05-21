using System.IO;

namespace GDScriptConverter
{
    internal class GDScriptParser
    {
        public GDProject Project { get; } = new GDProject();

        public GDScriptParser()
        {
        }

        public GDNode Parse(string filePath)
        {
            var state = new GDReadingState(Project);

            state.FileStarted();

            foreach (var line in File.ReadLines(filePath))
                ParseLine(line, state);

            state.FileFinished();

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