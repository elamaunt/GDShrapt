using System;
using System.Collections.Generic;
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

        public GDClassDeclaration ParseFile(string filePath)
        {
            var state = new GDReadingState(Settings);

            var declaration = new GDClassDeclaration();
            state.Push(declaration);

            var buffer = new char[Settings.ReadBufferSize];
            int count = 0;

            using(var stream = File.OpenRead(filePath))
            using (var reader = new StreamReader(stream))
            {
                while ((count = reader.Read(buffer, 0, buffer.Length)) > 0)
                    ParseBuffer(buffer, count, state);
            }

            state.CompleteReading();

            return declaration;
        }

        public GDExpression ParseExpression(string content)
        {
            var state = new GDReadingState(Settings);
            var receiver = new GDReceiver();

            state.Push(new GDStartTrimmingResolver(receiver, () => new GDExpressionResolver(receiver)));

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

        public GDStatement ParseStatement(string content)
        {
            var state = new GDReadingState(Settings);
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

        public List<GDSyntaxToken> ParseUnspecifiedContent(string content)
        {
            var state = new GDReadingState(Settings);
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

        private void ParseBuffer(char[] buffer, int count, GDReadingState state)
        {
            for (int i = 0; i < count; i++)
                state.PassChar(buffer[i]);
        }
    }
}