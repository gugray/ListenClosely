using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Xml;

namespace WiktionaryParser
{
    class DumpPageReader : IDisposable
    {
        StreamReader sr = null;
        XmlReader xr = null;

        public DumpPageReader(string fn)
        {
            try
            {
                sr = new StreamReader(fn);
                xr = XmlReader.Create(sr);
            }
            catch { Dispose(); throw; }
        }

        public void Dispose()
        {
            if (xr != null) { xr.Dispose(); xr = null; }
            if (sr != null) { sr.Dispose(); sr = null; }
        }

        string currTitle = "";
        string currText = "";
        bool lInPage;
        List<string> lElmStack = new List<string>();

        void startElement(string name, Dictionary<string, string> attributes)
        {
            lElmStack.Add(name);
            if (name == "page") lInPage = true;
        }

        bool endElement(string name)
        {
            lElmStack.RemoveAt(lElmStack.Count - 1);
            if (name == "page") return true;
            return false;
        }

        void characters(string text)
        {
            if (lInPage && lElmStack[lElmStack.Count - 1] == "title") currTitle += text;
            else if (lInPage && lElmStack[lElmStack.Count - 1] == "text") currText += text;
        }

        bool loopForPage()
        {
            bool foundPageClose = false;
            currText = currTitle = "";
            lInPage = false;
            while (!foundPageClose && xr.Read())
            {
                switch (xr.NodeType)
                {
                    case XmlNodeType.Element:
                        string name = xr.Name;
                        Dictionary<string, string> attributes = new Dictionary<string, string>();
                        if (xr.HasAttributes)
                        {
                            for (int i = 0; i < xr.AttributeCount; i++)
                            {
                                xr.MoveToAttribute(i);
                                attributes.Add(xr.Name, xr.Value);
                            }
                            // TO-DO: verify
                            // Discovered by DA
                            //reader.MoveToElement();
                        }
                        startElement(name, attributes);
                        if (xr.IsEmptyElement) foundPageClose = endElement(xr.Name);
                        break;
                    case XmlNodeType.EndElement:
                        foundPageClose = endElement(xr.Name);
                        break;
                    case XmlNodeType.Text:
                        characters(xr.Value);
                        break;
                    case XmlNodeType.Whitespace:
                        characters(xr.Value);
                        break;
                }
            }
            return foundPageClose;
        }

        public DumpPage GetNextPage()
        {
            if (!loopForPage()) return null;
            return new DumpPage
            {
                Title = currTitle,
                Text = currText,
            };
        }
    }
}
