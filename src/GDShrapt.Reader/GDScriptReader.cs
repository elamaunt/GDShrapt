using System.IO;

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
            state.PushNode(declaration);

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
            state.PushNode(declaration);

            foreach (var line in File.ReadLines(filePath))
                ParseLine(line, state);

            state.CompleteReading();

            return declaration;
        }

        public GDExpression ParseExpression(string content)
        {
            GDExpression expression = null;
            
            var state = new GDReadingState(Settings);

            state.PushNode(new GDExpressionResolver(expr => expression = expr));

            using (var reader = new StringReader(content))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                    ParseLine(line, state);
            }

            state.CompleteReading();
            return expression;
        }

        public GDStatement ParseStatement(string content)
        {
            GDStatement statement = null;

            var state = new GDReadingState(Settings);

            state.PushNode(new GDStatementResolver(0, st => statement = st));

            using (var reader = new StringReader(content))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                    ParseLine(line, state);
            }

            state.CompleteReading();
            return statement;
        }

        private void ParseLine(string line, GDReadingState state)
        {
            for (int i = 0; i < line.Length; i++)
                state.PassChar(line[i]);

            state.PassLineFinish();
        }
    }
}