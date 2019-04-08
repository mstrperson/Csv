using System;
using System.Collections.Generic;

namespace Csv
{
    public interface IConflictRule
    {
        string Resolve(Row dataA, Row dataB, string conflictColumn);
    }

    public class UserResolveConflictRule : IConflictRule
    {
        public string Resolve(Row dataA, Row dataB, string conflictColumn)
        {
            Console.WriteLine("Conflicting Data in column '{0}'.  Choose which data to keep.", conflictColumn);
            Console.WriteLine("1) {0}\n2) {1}\n3) *something else*", dataA[conflictColumn], dataB[conflictColumn]);
            Console.Write(">> ");

            string choice = Console.ReadLine();
            if (choice.Equals("1")) return dataA[conflictColumn];
            else if (choice.Equals("2")) return dataB[conflictColumn];
            else
            {
                Console.WriteLine("Enter a value to replace.");
                Console.Write(">> ");
                return Console.ReadLine();
            }
        }
    }

    /// <summary>
    /// This is a dummy rule for throwing away extra data that you don't care about.
    /// No comparison is made.  This rule just returns data with what ever is in the dataA parameter of the conflict column.
    /// </summary>
    public class PickTheFirstConflictRule : IConflictRule
    {
        /// <summary>
        /// Does not compare anything. Just returns the data in the first parameter
        /// </summary>
        /// <returns>The resolve.</returns>
        /// <param name="dataA">Data a.</param>
        /// <param name="dataB">Data b.</param>
        /// <param name="conflictColumn">Conflict column.</param>
        public string Resolve(Row dataA, Row dataB, string conflictColumn)
        {
            return dataA[conflictColumn];
        }
    }

    public class YesBeatsNoConflictRule : IConflictRule
    {
        /// <summary>
        /// The next rule in the conflict rule chain.
        /// </summary>
        public IConflictRule NextRule;

        /// <summary>
        /// Check to see if Yes or No, and Yes wins!
        /// otherwise, hand off to the next rule
        /// </summary>
        /// <returns>The resolve.</returns>
        /// <param name="dataA">Data a.</param>
        /// <param name="dataB">Data b.</param>
        /// <param name="conflictColumn">Conflict column.</param>
        public string Resolve(Row dataA, Row dataB, string conflictColumn)
        {
            if (dataA[conflictColumn].ToLower().Equals("n") && dataB[conflictColumn].ToLower().Equals("y")) return "y";
            if (dataB[conflictColumn].ToLower().Equals("n") && dataA[conflictColumn].ToLower().Equals("y")) return "y";

            if (dataA[conflictColumn].ToLower().Equals("no") && dataB[conflictColumn].ToLower().Equals("yes")) return "yes";
            if (dataB[conflictColumn].ToLower().Equals("no") && dataA[conflictColumn].ToLower().Equals("yes")) return "yes";

            if (NextRule == null) throw new ConflictRuleException("YesBeatsNo rule does not apply and NextRule is not defined.");

            return NextRule.Resolve(dataA, dataB, conflictColumn);
        }
    }

    /// <summary>
    /// Conflict rule exception.
    /// </summary>
    public class ConflictRuleException : Exception
    {
        /// <summary>
        /// Nothing special here, just indicates a exception involving a conflict rule.
        /// </summary>
        /// <param name="message">Message.</param>
        /// <param name="innerException">Inner exception.</param>
        public ConflictRuleException(string message, Exception innerException = null) : base(message, innerException)
        {
            
        }
    }
}
