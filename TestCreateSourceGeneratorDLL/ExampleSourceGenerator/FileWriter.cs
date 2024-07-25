using System.Collections.Generic;

namespace PolymorphicStructsSourceGenerators
{
    internal class FileWriter
    {
        public string FileContents = "";
        private int _indentLevel;
        private bool _hasNamespace;

        public void WriteLine(string line) =>
            this.FileContents = this.FileContents + this.GetIndentString() + line + "\n";

        public void WriteUsings(List<string> usings)
        {
            List<string> stringList = new List<string>();
            foreach (string str in usings)
            {
                if (!string.IsNullOrEmpty(str) && !string.IsNullOrWhiteSpace(str) && !stringList.Contains(str))
                    stringList.Add(str);
            }

            foreach (string str in stringList)
                this.WriteLine("using " + str + ";");
        }

        public void BeginScope()
        {
            this.WriteLine("{");
            ++this._indentLevel;
        }

        public void EndScope(string suffix = "")
        {
            --this._indentLevel;
            this.WriteLine("}" + suffix);
        }

        public void BeginNamespace(string namespaceName)
        {
            if (string.IsNullOrEmpty(namespaceName))
                return;
            this._hasNamespace = true;
            this.WriteLine("namespace " + namespaceName);
            this.BeginScope();
        }

        public void EndNamespace()
        {
            if (!this._hasNamespace)
                return;
            this.EndScope();
        }

        private string GetIndentString()
        {
            string indentString = "";
            for (int index = 0; index < this._indentLevel; ++index)
                indentString += "\t";
            return indentString;
        }
    }
}