using System.IO;
using System.Linq;

namespace GDShrapt.Reader
{
    public class GDScriptReader
    {
        public static readonly GDReadSettings DefaultSettings = new GDReadSettings();

        public GDReadSettings Settings { get; }

        public GDScriptReader()
        {
            Settings = DefaultSettings;
        }

        public GDScriptReader(GDReadSettings settings)
        {
            Settings = settings;
        }

        public GDClassDeclaration ParseFileContent(string content)
        {
            var state = new GDReadingState(Settings);

            var declaration = new GDClassDeclaration();
            state.Push(declaration);

            using (var reader = new StringReader(content))
            {
                string line;
                while((line = reader.ReadLine()) != null)
                    ParseLine(line, state);
            }

            state.CompleteReading();

            return declaration;
        }

        public GDClassDeclaration ParseFile(string filePath)
        {
            var state = new GDReadingState(Settings);

            var declaration = new GDClassDeclaration();
            state.Push(declaration);

            foreach (var line in File.ReadLines(filePath))
                ParseLine(line, state);

            state.CompleteReading();

            return declaration;
        }

        public GDExpression ParseExpression(string content)
        {
            var state = new GDReadingState(Settings);
            var receiver = new GDReceiver();

            state.Push(new GDExpressionResolver(receiver));

            using (var reader = new StringReader(content))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                    ParseLine(line, state);
            }

            state.CompleteReading();
            return receiver.Tokens.OfType<GDExpression>().FirstOrDefault();
        }

        public GDStatement ParseStatement(string content)
        {
            var state = new GDReadingState(Settings);
            var receiver = new GDReceiver();

            state.Push(new GDStatementResolver(receiver, 0));

            using (var reader = new StringReader(content))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                    ParseLine(line, state);
            }

            state.CompleteReading();
            return receiver.Tokens.OfType<GDStatement>().FirstOrDefault();
        }

        private void ParseLine(string line, GDReadingState state)
        {
            for (int i = 0; i < line.Length; i++)
                state.PassChar(line[i]);

            state.PassLineFinish();
        }
    }
}