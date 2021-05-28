using System.IO;

namespace GDSharp.Reader
{
    public class GDScriptReader
    {
        public GDTypeDeclaration ParseFileContent(string content)
        {
            var state = new GDReadingState();

            state.PushNode(new GDTypeDeclarationResolver(state));

            using (var reader = new StringReader(content))
            {
                string line;
                while((line = reader.ReadLine()) != null)
                    ParseLine(line, state);
            }

            state.CompleteReading();

            return state.Type;
        }

        public GDTypeDeclaration ParseFile(string filePath)
        {
            var state = new GDReadingState();

            state.PushNode(new GDTypeDeclarationResolver(state));

            foreach (var line in File.ReadLines(filePath))
                ParseLine(line, state);

            state.CompleteReading();

            return state.Type;
        }

        public GDExpression ParseExpression(string content)
        {
            GDExpression expression = null;
            
            var state = new GDReadingState();

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

        private void ParseLine(string line, GDReadingState state)
        {
            for (int i = 0; i < line.Length; i++)
                state.HandleChar(line[i]);

            state.FinishLine();
        }
    }
}