using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections;

/// <summary>
/// Summary description for CSV
/// </summary>
namespace Csv
{
    public class Row : Dictionary<string,string>
    {
        /// <summary>
        /// extend the Dictionary Array Index Operator for a CSV Row.
        /// </summary>
        /// <param name="key">row header</param>
        /// <returns></returns>
        public new string this[string key]
        {
            get
            {
                if (this.ContainsKey(key))
                    return base[key];
                else
                    return "";
            }
            set
            {
                if (this.ContainsKey(key))
                    base[key] = value;
                else
                    this.Add(key, value);
            }
        }

        public string JsonString
        {
            get
            {
                bool first = true;
                string json = "{";
                foreach(string key in this.Keys)
                {
                    json += string.Format("{2}\"{0}\":\"{1}\"", key, this[key], first?"\n\t": ",\n\t");
                    first = false;
                }
                json += "\n}";

                return json;
            }
        }
    }

    public class CSV : IEnumerable<Row>
    {
        /// <summary>
        /// A title or heading for the table.  If you want one...
        /// </summary>
        public string Heading
        { get; set; }

        protected List<Row> _Data;

        /// <summary>
        /// Readonly access to all of the rows of this table.
        /// </summary>
        public List<Row> Data
        {
            get { return _Data; }
        }

        /// <summary>
        /// Gets the Enumerator for iterrating over this CSV.
        /// Enumerates the Rows of this CSV.
        /// </summary>
        /// <returns></returns>
        public IEnumerator<Row> GetEnumerator()
        {
            return ((IEnumerable<Row>)Data).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<Row>)Data).GetEnumerator();
        }

        /// <summary>
        /// Readonly access to the data in this table by row number.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public Row this[int index]
        {
            get
            {
                return this.Data[index];
            }
        }

        public string JsonString
        {
            get
            {
                string json = "[";
                bool first = true;
                foreach (Row row in this)
                {
                    json += string.Format("{0}{1}", first ? "\n\t" : ",\n\t", row.JsonString.Replace("\n", "\n\t"));
                    first = false;
                }

                json += "\n]";
                return json;
            }
        }

        public string HtmlTable(string tableCssClass = "", string headerRowCssClass = "", string rowCssClass = "")
        {
            string html = string.Format("<table{0}>{2}<tr{1}>",
                string.IsNullOrEmpty(tableCssClass) ? "" : string.Format(" class=\"{0}\"", tableCssClass),
                string.IsNullOrEmpty(headerRowCssClass) ? "" : string.Format(" class=\"{0}\"", headerRowCssClass),
                string.IsNullOrEmpty(Heading)? "" : string.Format("<thead>{0}</thead>", Heading));
            foreach(string header in AllKeys)
            {
                html += string.Format("<th>{0}</th>", header);
            }

            html += "</tr>";

            foreach(Row row in this)
            {
                html += string.Format("<tr{0}>", string.IsNullOrEmpty(rowCssClass) ? "" : string.Format(" class=\"{0}\"", rowCssClass));
                foreach(string header in AllKeys)
                {
                    html += string.Format("<td>{0}</td>", row[header]);
                }
                html += "</tr>";
            }

            html += "</table>";
            return html;
        }

        /// <summary>
        /// Query this table and find all rows that match a set of values.
        /// </summary>
        /// <param name="primaryKey"></param>
        /// <returns></returns>
        public CSV this[Row primaryKey]
        {
            get
            {
                CSV output = new CSV();
                foreach (Row row in this)
                {
                    foreach (string key in primaryKey.Keys)
                    {
                        if (!row.ContainsKey(key) || !row[key].Equals(primaryKey[key]))
                        {
                            continue;
                        }
                    }

                    output.Add(row);
                }

                return output;
            }
        }

        /// <summary>
        /// Query this table and find all rows that match a set of regular expressions.
        /// Use this to find all rows where a data fits a particular set of regular expressions.
        /// for example, find all rows in a contact list where the Phone Number has a 540 area code
        /// and the street address is in a particular town.
        /// </summary>
        /// <param name="primaryKey"></param>
        /// <returns></returns>
        public CSV this[Dictionary<string, Regex> primaryKey]
        {
            get
            {
                CSV output = new CSV();
                foreach (Row row in this)
                {
                    foreach (string key in primaryKey.Keys)
                    {
                        if (!row.ContainsKey(key) || !primaryKey[key].IsMatch(row[key]))
                        {
                            continue;
                        }
                    }

                    output.Add(row);
                }

                return output;
            }
        }

        /// <summary>
        /// Quick get Column by name.
        /// </summary>
        /// <param name="column"></param>
        /// <returns></returns>
        public List<string> this[string column] => this.GetColumn(column);

        public bool ContainsNulls(string column)
        {
            List<string> col = this.GetColumn(column);
            foreach (string val in col)
                if (val.ToLowerInvariant().Equals("null"))
                    return true;

            return false;
        }

        public string GuessMySQLDataType(string column)
        {
            string type = "TEXT";
            List<string> col = this.GetColumn(column);

            Regex interger = new Regex(@"^(null|-?\d+)$");
            Regex floating = new Regex(@"^(null|-?\d*\.?\d+)$");

            bool maybeInt = true;
            bool maybeDouble = true;
            bool maybeDateTime = true;

            int longest = 0;

            bool containsNulls = false;

            foreach(string value in col)
            {
                if (value.Length > longest) longest = value.Length;
                
                string lval = value.ToLowerInvariant();
                if (lval.Equals("privacysuppressed")) lval = "null";
                if (lval.Equals("null")) containsNulls = true;

                if (maybeInt && !interger.IsMatch(lval))
                    maybeInt = false;

                if (maybeDouble && !floating.IsMatch(lval))
                    maybeDouble = false;

                DateTime dt;
                if (maybeDateTime && !lval.Equals("null") && !DateTime.TryParse(value, out dt))
                    maybeDateTime = false;
            }

            if (maybeDateTime) type = "DATETIME";
            else if (maybeInt) type = "INT";
            else if (maybeDouble) type = "DOUBLE";

            else if(longest <= 255)
                type = "varchar(255)";

            if (containsNulls)
                type += " NULL";
            else
                type += " NOT NULL";

            return type;
        }

        /// <summary>
        /// Shortcut for GetRow.
        /// </summary>
        /// <param name="column"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public Row this[string column, string value] => this.GetRow(column, value);

        protected static Regex Quoted = new Regex("^\"[^\"]*\"$");

        /// <summary>
        /// Initialize a blank CSV.
        /// </summary>
        /// <param name="heading"></param>
        public CSV(string heading = "")
        {
            Heading = heading;
            _Data = new List<Row>();
        }

        /// <summary>
        /// Initialize a CSV from a List of Dictionaries.
        /// </summary>
        /// <param name="data"></param>
        public CSV(List<Row> data)
        {
            Heading = "";
            _Data = data;
        }

        /// <summary>
        /// Open a CSV from a stream.
        /// </summary>
        /// <param name="inputStream"></param>
        public CSV(Stream inputStream)
        {
            Heading = "";
            _Data = new List<Row>();
            StreamReader reader = new StreamReader(inputStream);
            string line = reader.ReadLine();
            string[] headers = line.Split(',');
            while (!reader.EndOfStream)
            {
                Row row = new Row();
                line = reader.ReadLine();
                string[] values = line.Split(',');
                for (int i = 0; i < headers.Length && i < values.Length; i++)
                {
                    if (Quoted.IsMatch(values[i]))
                    {
                        values[i] = values[i].Substring(1, values[i].Length - 2);
                    }

                    while (row.ContainsKey(headers[i])) headers[i] = headers[i] + " ";

                    row.Add(headers[i], values[i]);
                }

                _Data.Add(row);
            }
        }

        /// <summary>
        /// Add a row to this CSV.
        /// Resets the AllKeys field.
        /// </summary>
        /// <param name="row"></param>
        public void Add(Row row)
        {
            _Data.Add(row);
            _AllKeys = new List<string>();
        }


        /// <summary>
        /// Check to see if a row is in this CSV.
        /// </summary>
        /// <param name="row"></param>
        /// <returns></returns>
        public bool Contains(Row row)
        {
            for (int i = 0; i < _Data.Count; i++)
            {
                bool match = true;
                foreach (string key in row.Keys)
                {
                    if (!_Data[i].ContainsKey(key))
                    {
                        match = false;
                        break;
                    }

                    if (!_Data[i][key].Equals(row[key]))
                    {
                        match = false;
                        break;
                    }
                }

                if (match)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Get a CSV containing all the entries of this CSV which do not correspond to entries in the Other CSV.
        /// </summary>
        /// <param name="Other"></param>
        /// <returns></returns>
        public CSV NotIn(CSV Other)
        {
            CSV newCSV = new CSV();

            List<string> CommonKeys = new List<string>();
            foreach (string key in AllKeys)
            {
                if (Other.AllKeys.Contains(key))
                {
                    CommonKeys.Add(key);
                }
            }

            foreach (Row row in _Data)
            {
                Row strippedRow = new Row();
                foreach (string key in CommonKeys)
                {
                    strippedRow.Add(key, row[key]);
                }

                if (!Other.Contains(strippedRow))
                {
                    newCSV.Add(row);
                }
            }

            return newCSV;
        }

        /// <summary>
        /// Remove a row from this CSV.
        /// </summary>
        /// <param name="row"></param>
        public void Remove(Row row)
        {
            #region Search
            int index = -1;
            bool foundMatch = false;
            do
            {
                foundMatch = false;
                for (int i = 0; i < _Data.Count; i++)
                {
                    bool match = true;
                    foreach (string key in row.Keys)
                    {
                        if (!_Data[i].ContainsKey(key))
                        {
                            match = false;
                            break;
                        }

                        if (!_Data[i][key].Equals(row[key]))
                        {
                            match = false;
                            break;
                        }
                    }

                    if (match)
                    {
                        index = i;
                        foundMatch = true;
                        break;
                    }
                }

                if (index != -1)
                    _Data.RemoveAt(index);
            } while (foundMatch);
            #endregion

            _AllKeys = new List<string>();
        }

        private List<string> _AllKeys;

        /// <summary>
        /// Get the list of all keys in this CSV.
        /// Not every row is guaranteed to have a value for every key.
        /// </summary>
        public List<string> AllKeys
        {
            get
            {
                if (_AllKeys == null || _AllKeys.Count == 0)
                {
                    _AllKeys = new List<string>();

                    foreach (Row row in _Data)
                    {
                        foreach (string key in row.Keys)
                        {
                            if (!_AllKeys.Contains(key))
                            {
                                _AllKeys.Add(key);
                            }
                        }
                    }
                }
                return _AllKeys;
            }
        }


        /// <summary>
        /// How many Columns are in this CSV?
        /// </summary>
        public int ColCount
        {
            get
            {
                return AllKeys.Count;
            }
        }

        /// <summary>
        /// How many rows are in this CSV?
        /// </summary>
        public int RowCount
        {
            get
            {
                return Data.Count;
            }
        }

        /// <summary>
        /// Save the CSV to a file.
        /// This method will delete an existing file with this name.
        /// </summary>
        /// <param name="fileName"></param>
        public void Save(string fileName)
        {
            if (File.Exists(fileName)) File.Delete(fileName);
            this.Save(new FileStream(fileName, FileMode.OpenOrCreate));
        }

        /// <summary>
        /// Save the CSV to a stream.
        /// </summary>
        /// <param name="output"></param>
        public void Save(Stream output)
        {
            StreamWriter writer = new StreamWriter(output);
            writer.AutoFlush = true;

            if (!Heading.Equals(""))
                writer.WriteLine(Heading);

            if (AllKeys.Count <= 0)
            {
                return;
            }

            writer.Write(AllKeys[0]);
            for (int i = 1; i < AllKeys.Count; i++)
            {
                writer.Write(",{0}", AllKeys[i]);
            }
            writer.WriteLine();

            foreach (Row row in _Data)
            {
                if (row.ContainsKey(AllKeys[0]))
                    writer.Write(row[AllKeys[0]]);
                for (int i = 1; i < AllKeys.Count; i++)
                {
                    writer.Write(",");
                    if (row.ContainsKey(AllKeys[i]))
                    {
                        writer.Write(row[AllKeys[i]]);
                    }
                }
                writer.WriteLine();
            }


            writer.Close();
        }

        /// <summary>
        /// Bulk add data from another CSV object.
        /// </summary>
        /// <param name="other"></param>
        public void Add(CSV other)
        {
            foreach (Row row in other.Data)
            {
                this.Add(row);
            }
        }

        /// <summary>
        /// Get all of the data in the requested column of the table.
        /// if an invalid header is given, throws an ArgumentOutOfRangeException.
        /// </summary>
        /// <param name="header">Column Header from this table.</param>
        /// <returns></returns>
        public List<string> GetColumn(string header)
        {
            if (!AllKeys.Contains(header))
            {
                header = string.Format("\"{0}\"", header);
                if(!AllKeys.Contains(header))
                    throw new ArgumentOutOfRangeException("Invalid Header Name");
            }
            List<string> column = new List<string>();
            foreach (Row row in Data)
            {
                if (!row.ContainsKey(header)) column.Add("");
                else column.Add(row[header]);
            }

            return column;
        }

        /// <summary>
        /// Get the first row matching the given headr, key pair.
        /// if no row under the given header contains the given key, 
        /// throws an ArgumentOutOfRangeException.
        /// 
        /// </summary>
        /// <param name="header"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public Row GetRow(string header, string key)
        {
            foreach (Row row in Data)
            {
                if (!row.ContainsKey(header)) continue;

                if (row[header].Equals(key))
                {
                    return row;
                }
            }

            throw new ArgumentOutOfRangeException(string.Format("{0} was not found under the {1} header.", key, header));
        }
    }
}
