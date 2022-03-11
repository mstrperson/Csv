using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Csv
{
    public interface INormalizationRule
    {
        void Normalize(ref List<Row> data, List<string> columns);
    }


    public class SerialNumberNormalizationRule : INormalizationRule
    {
        public bool Capitalize { get; set; }

        /// <summary>
        /// Normalize the specified columns of the given data.
        /// Standardizes data as all caps or all lowercase depending on the status of the Capitalize property.
        /// </summary>
        /// <param name="data">Data.</param>
        /// <param name="columns">Columns.</param>
        public void Normalize(ref List<Row> data, List<string> columns)
        {
            foreach(Row row in data)
            {
                foreach(string column in columns)
                {
                    row[column] = Capitalize ? row[column].ToUpperInvariant() : row[column].ToLowerInvariant();
                }
            }
        }
    }

    /// <summary>
    /// MAC Address normalization rule.
    /// </summary>
    public class MACAddressNormalizationRule : INormalizationRule
    {
        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="T:Csv.MACAddressNormalizationRule"/>
        /// is all caps.
        /// </summary>
        /// <value><c>true</c> if capitalize; otherwise, <c>false</c>.</value>
        public bool Capitalize { get; set; }
        public enum MacSeparator
        {
            None = 0,
            Colon = 1,
            Dash = 2
        }

        private string[] separators = { "", ":", "-" };

        /// <summary>
        /// Gets or sets the standard separator.
        /// </summary>
        /// <value>The separator.</value>
        public MacSeparator Separator { get; set; }

        /// <summary>
        /// Normalize the MAC Addresses in the specified columns of data according to the configured standards.
        /// </summary>
        /// <param name="data">Data.</param>
        /// <param name="columns">Columns.</param>
        public void Normalize(ref List<Row> data, List<string> columns)
        {
            Regex macaddress = new Regex(MatchPattern);
            foreach(Row row in data)
            {
                foreach(string column in columns)
                {
                    if (macaddress.IsMatch(row[column]))
                        continue;

                    string stripped = "";
                    Regex digit = new Regex("[0-9a-fA-F]");

                    foreach(char ch in row[column])
                    {
                        if(digit.IsMatch("" + ch))
                        {
                            stripped += ch;
                        }
                    }

                    stripped = Capitalize ? stripped.ToUpperInvariant() : stripped.ToLowerInvariant();

                    if (stripped.Length != 12)
                        continue;
                        //throw new FormatException("Invalid MAC Address");

                    if( Separator == MacSeparator.None) 
                    {
                        row[column] = stripped;
                        continue;
                    }

                    string formatted = "";
                    for (int i = 0; i < 12; i+=2)
                    {
                        formatted += stripped[i];
                        formatted += stripped[i + 1];
                        if (i < 9) formatted += separators[(int)Separator];
                    }

                    row[column] = formatted;
                }
            }
        }

        public string ApplyNormalization(string mac)
        {
            if (IsValid(mac))
                return mac;

            string stripped = "";
            Regex digit = new Regex("[0-9a-fA-F]");

            foreach (char ch in mac)
            {
                if (digit.IsMatch("" + ch))
                {
                    stripped += ch;
                }
            }

            stripped = Capitalize ? stripped.ToUpperInvariant() : stripped.ToLowerInvariant();

            if (Separator == MacSeparator.None) 
                return stripped;

            string formatted = "";
            for (int i = 0; i < 12; i += 2)
            {
                formatted += stripped[i];
                formatted += stripped[i + 1];
                if (i < 9) formatted += separators[(int)Separator];
            }

            return formatted;
        }

        public bool IsValid(string mac)
        {
            Regex macaddress = new Regex(MatchPattern);
            return macaddress.IsMatch(mac);
        }

        /// <summary>
        /// Gets the match pattern corresponding to a mac address with the 
        /// configured standard for this instance.
        /// </summary>
        /// <value>Regex pattern.</value>
        protected string MatchPattern
        {
            get
            {
                string letter = Capitalize ? "A-F" : "a-f";
                if(Separator == MacSeparator.None)
                {
                    return "^[0-9" + letter + "]{12}$";
                }

                return "^([0-9" + letter + "]{2}" + separators[(int)Separator] + "){5}[" + letter + "0-9]{2}$";
            }
        }
    }
}
