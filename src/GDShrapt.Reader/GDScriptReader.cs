using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

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

        #region ParseFileContent

        public GDClassDeclaration ParseFileContent(string content)
        {
            return ParseFileContent(content, CancellationToken.None);
        }

        public GDClassDeclaration ParseFileContent(string content, CancellationToken cancellationToken)
        {
            var state = new GDReadingState(Settings, cancellationToken);

            var declaration = new GDClassDeclaration();
            state.Push(declaration);

            var buffer = new char[Settings.ReadBufferSize];
            int count = 0;

            using (var reader = new StringReader(content))
            {
                while ((count = reader.Read(buffer, 0, buffer.Length)) > 0)
                    ParseBuffer(buffer, count, state);
            }

            state.CompleteReading();

            return declaration;
        }

        #endregion

        #region ParseFile

        public GDClassDeclaration ParseFile(string filePath)
        {
            return ParseFile(filePath, CancellationToken.None);
        }

        public GDClassDeclaration ParseFile(string filePath, CancellationToken cancellationToken)
        {
            var state = new GDReadingState(Settings, cancellationToken);

            var declaration = new GDClassDeclaration();
            state.Push(declaration);

            var buffer = new char[Settings.ReadBufferSize];
            int count = 0;

            using (var stream = File.OpenRead(filePath))
            using (var reader = new StreamReader(stream))
            {
                while ((count = reader.Read(buffer, 0, buffer.Length)) > 0)
                    ParseBuffer(buffer, count, state);
            }

            state.CompleteReading();

            return declaration;
        }

        #endregion

        #region ParseExpression

        public GDExpression ParseExpression(string content)
        {
            return ParseExpression(content, CancellationToken.None);
        }

        public GDExpression ParseExpression(string content, CancellationToken cancellationToken)
        {
            var state = new GDReadingState(Settings, cancellationToken);
            var receiver = new GDReceiver();

            state.Push(new GDStartTrimmingResolver(receiver, () => new GDExpressionResolver(receiver, 0)));

            var buffer = new char[Settings.ReadBufferSize];
            int count = 0;

            using (var reader = new StringReader(content))
            {
                while ((count = reader.Read(buffer, 0, buffer.Length)) > 0)
                    ParseBuffer(buffer, count, state);
            }

            state.CompleteReading();
            return receiver.Tokens.OfType<GDExpression>().FirstOrDefault();
        }

        #endregion

        #region ParseStatement

        public GDStatement ParseStatement(string content)
        {
            return ParseStatement(content, CancellationToken.None);
        }

        public GDStatement ParseStatement(string content, CancellationToken cancellationToken)
        {
            var state = new GDReadingState(Settings, cancellationToken);
            var receiver = new GDReceiver();

            state.Push(new GDStatementsResolver(receiver, 0));

            var buffer = new char[Settings.ReadBufferSize];
            int count = 0;

            using (var reader = new StringReader(content))
            {
                while ((count = reader.Read(buffer, 0, buffer.Length)) > 0)
                    ParseBuffer(buffer, count, state);
            }

            state.CompleteReading();
            return receiver.Tokens.OfType<GDStatement>().FirstOrDefault();
        }

        #endregion

        #region ParseStatementsList

        public GDStatementsList ParseStatementsList(string content)
        {
            return ParseStatementsList(content, CancellationToken.None);
        }

        public GDStatementsList ParseStatementsList(string content, CancellationToken cancellationToken)
        {
            var state = new GDReadingState(Settings, cancellationToken);
            var list = new GDStatementsList();

            state.Push(list);

            var buffer = new char[Settings.ReadBufferSize];
            int count = 0;

            using (var reader = new StringReader(content))
            {
                while ((count = reader.Read(buffer, 0, buffer.Length)) > 0)
                    ParseBuffer(buffer, count, state);
            }

            state.CompleteReading();
            return list;
        }

        #endregion

        #region ParseStatements

        public List<GDStatement> ParseStatements(string content)
        {
            return ParseStatements(content, CancellationToken.None);
        }

        public List<GDStatement> ParseStatements(string content, CancellationToken cancellationToken)
        {
            var state = new GDReadingState(Settings, cancellationToken);
            var receiver = new GDReceiver();

            state.Push(new GDStatementsResolver(receiver, 0));

            var buffer = new char[Settings.ReadBufferSize];
            int count = 0;

            using (var reader = new StringReader(content))
            {
                while ((count = reader.Read(buffer, 0, buffer.Length)) > 0)
                    ParseBuffer(buffer, count, state);
            }

            state.CompleteReading();
            return receiver.Tokens.OfType<GDStatement>().ToList();
        }

        #endregion

        #region ParseUnspecifiedContent

        public List<GDSyntaxToken> ParseUnspecifiedContent(string content)
        {
            return ParseUnspecifiedContent(content, CancellationToken.None);
        }

        public List<GDSyntaxToken> ParseUnspecifiedContent(string content, CancellationToken cancellationToken)
        {
            var state = new GDReadingState(Settings, cancellationToken);
            var receiver = new GDReceiver();

            state.Push(new GDContentResolver(receiver));

            var buffer = new char[Settings.ReadBufferSize];
            int count = 0;

            using (var reader = new StringReader(content))
            {
                while ((count = reader.Read(buffer, 0, buffer.Length)) > 0)
                    ParseBuffer(buffer, count, state);
            }

            state.CompleteReading();
            return receiver.Tokens;
        }

        #endregion

        #region ParseType

        public GDTypeNode ParseType(string type)
        {
            return ParseType(type, CancellationToken.None);
        }

        public GDTypeNode ParseType(string type, CancellationToken cancellationToken)
        {
            var state = new GDReadingState(Settings, cancellationToken);
            var receiver = new GDReceiver();

            state.Push(new GDTypeResolver(receiver));

            var buffer = new char[Settings.ReadBufferSize];
            int count = 0;

            using (var reader = new StringReader(type))
            {
                while ((count = reader.Read(buffer, 0, buffer.Length)) > 0)
                    ParseBuffer(buffer, count, state);
            }

            state.CompleteReading();
            return receiver.Tokens.OfType<GDTypeNode>().FirstOrDefault();
        }

        #endregion

        private void ParseBuffer(char[] buffer, int count, GDReadingState state)
        {
            for (int i = 0; i < count; i++)
                state.PassChar(buffer[i]);
        }
    }
}