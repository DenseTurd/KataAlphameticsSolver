using System;
using System.Collections.Generic;
using System.Linq;

namespace KataAlphameticsSolver
{
    /* If result longer than max addend length largest column must be 1.
     * If the sum of a column is not equal to the result column there must be a carry.
     * If none of the chars in the addend column are the same as the result column and there must be a carry
     * none of the addends in that column can be zero. ?? Really tho?
     * 
    */

    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine(Alphametics("AD + BA = CE"));
            //Console.WriteLine(Alphametics("MA + MA = ABB")); //"MA + MA = ABB" >> 61 + 61 = 122
        }

        static Dictionary<char, List<int>> charIntDict;
        static Dictionary<char, List<int>> tempCharIntDict;
        static string[] addends;
        static string result;
        static List<int> remainingValues = new List<int>() { 9, 8, 7, 6, 5, 4, 3, 2, 1, 0 };
        static Dictionary<int, int> columnSums;
        static int maxAddendLength;
        enum RequiresCarry
        {
            Yes,
            No,
            NotSure
        }
        static List<RequiresCarry> columnCarrys;
        static List<bool> columSolved;

        public static string Alphametics(string s)
        { 
            FormatStrings(s);
            CreateColumnCarrysList();
            CreateColumnSumDictionary();
            CreateColumnSolvedList();
            ResetCharIntDictionary();

            OptionallyWriteStringsToConsole();

            NoLeadingZeroes();

            SetFirstColumTo1IfResultLongerThanMaxAddendsLength();

            SetPotentialValuesForAddendsWithCharsInTheSameColums();

            TryFillValuesByAddingColumns();

            SystematicValueTestingAndAssigning();

            var outStr = charIntDict.Aggregate(s, (current, value) => current.Replace(value.Key, value.Value[0].ToString().ToCharArray()[0]));
            return "\nMy best guess: " + outStr;
        }

        static void NoLeadingZeroes()
        {
            for (int i = 0; i < addends.Length; i++)
            {
                charIntDict[addends[i][0]].Remove(0);
            }

            charIntDict[result[0]].Remove(0);
            Console.WriteLine("\nNo leading zeroes\n");
        }

        static void SystematicValueTestingAndAssigning()
        {
            Console.WriteLine("\nStarting algorithm\n");
            tempCharIntDict = charIntDict;
            for (int i = 0; i < result.Length; i++) // For each column
            {
                if (!AllValuesKnown(i))
                {
                    Console.WriteLine($"There are unknown values in column {i}\n");
                    ConstrainColumn(i);
                    if (!columSolved[i])
                    {
                        TryValuesForColumn(i);
                    }
                }
            }


            ShowMeWhatYouGot();
        }

        static void TryValuesForColumn(int column)
        {
            int addendSum = 0;
            for (int i = 0; i < addends.Length; i++)
            {
                int addendColumnIndex = (addends[i].Length - result.Length) + column;
                if (addendColumnIndex >= 0)
                {
                    if (tempCharIntDict[addends[i][addendColumnIndex]].Count != 1)
                    {
                        GetAndSetMinPossibleVal(tempCharIntDict, addends[i][addendColumnIndex]);
                        Console.WriteLine($"Setting char {addends[i][addendColumnIndex]} to {tempCharIntDict[addends[i][addendColumnIndex]][0]} for now");
                    }
                    addendSum += tempCharIntDict[addends[i][addendColumnIndex]][0];
                    //Console.WriteLine($"addend sum currently: {addendSum}");
                }
            }

            Console.WriteLine($"The addend sum for column {column} is: {addendSum}. Testing against result");
            TestAgainstResultColumn(addendSum, column);
        }

        static void TestAgainstResultColumn(int addendSum, int column)
        {
            if (tempCharIntDict[result[column]].Count == 1)
            {
                Console.WriteLine($"Column {column} of the result already has a value: {tempCharIntDict[result[column]][0]}");
                if (columnCarrys[column] == RequiresCarry.Yes)
                {
                    if (addendSum + 1 == tempCharIntDict[result[column]][0])
                    {
                        Console.WriteLine($"The values fit with a carry from the next column");
                        columSolved[column] = true;
                    }
                    else
                    {
                        Console.WriteLine($"The values dont fit with a carry");
                    }
                }
                else if (columnCarrys[column] == RequiresCarry.No)
                {
                    if (addendSum == tempCharIntDict[result[column]][0])
                    {
                        Console.WriteLine($"The values fit with no carry from the next column");
                        columSolved[column] = true;
                    }
                    else
                    {
                        Console.WriteLine($"The values dont fit with no carry");
                    }
                }
                else if (columnCarrys[column] == RequiresCarry.NotSure)
                {
                    if (addendSum == tempCharIntDict[result[column]][0] || addendSum + 1 == tempCharIntDict[result[column]][0])
                    {
                        Console.WriteLine($"The values fit but we are unsure about the carry from the next column");
                        columSolved[column] = true;
                    }
                    else
                    {
                        Console.WriteLine($"The values dont fit with or without a carry");
                    }
                }
            }
            else
            {
                Console.WriteLine($"Column {column} of the result does not have a value assigned");
                if (tempCharIntDict[result[column]].Contains(addendSum))
                {
                    Console.WriteLine($"Setting value to {addendSum} assuming no carry");

                    SetValForChar(tempCharIntDict, addendSum, result[column]);
                    columSolved[column] = true;
                }
                else if (tempCharIntDict[result[column]].Contains(addendSum + 1))
                {
                    Console.WriteLine($"Setting value to {addendSum +1} assuming carry");

                    SetValForChar(tempCharIntDict, addendSum + 1, result[column]);
                    columSolved[column] = true;
                }
            }
        }

        static void SetValForChar(Dictionary<char, List<int>> dict, int i, char ch)
        {
            RemoveEveryValueFromListExcept(dict[ch], i);
            RemoveValueFromEveryListInDictionaryExcept(i, dict, ch);
        }

        static void ConstrainColumn(int column)
        {
            var constraintTempDict = tempCharIntDict;
            if (column == 0)
            {
                Console.WriteLine("Working out sonstraints for column 0");
                // result[0] cannot be smaller than the sum of this columns minimum possible addends

                int columnAddendsSum = 0;
                for (int i = 0; i < addends.Length; i++)
                {
                    int addendColumnIndex = (addends[i].Length - result.Length) + column;
                    if (addendColumnIndex == 0)
                    {
                        GetAndSetMinPossibleVal(constraintTempDict, addends[i][0]);
                        Console.WriteLine($"{addends[i][0]} set to {constraintTempDict[addends[i][0]][0]}");
                        columnAddendsSum += constraintTempDict[addends[i][0]][0];
                    }
                }
                SetMinPossibleValFor(charIntDict, columnAddendsSum, result[0]);
                tempCharIntDict = charIntDict;
            }
        }

        static void SetMinPossibleValFor(Dictionary<char, List<int>> dict, int val, char ch)
        {
            for (int i = val -1; i >= 0; i--)
            {
                if (dict[ch].Contains(i))
                {
                    dict[ch].Remove(i);
                }
            }
        }

        static void GetAndSetMinPossibleVal(Dictionary<char, List<int>> dict, char ch)
        {
            RemoveEveryValueFromListExcept(dict[ch], dict[ch].Min());
            RemoveValueFromEveryListInDictionaryExcept(dict[ch].Min(), dict, ch);
        }

        private static void RemoveValueFromEveryListInDictionaryExcept(int val, Dictionary<char, List<int>> dict, char ch)
        {
            foreach (var list in dict)
            {
                if (list.Key != ch)
                {
                    if (list.Value.Contains(val))
                    {
                        list.Value.Remove(val);
                    }
                }
            }
        }

        static bool AllValuesKnown(int column)
        {
            Console.WriteLine($"\nChecking column: {column}");
            for (int i = 0; i < addends.Length; i++)
            {
                int addendColumnIndex = (addends[i].Length - result.Length) + column;
                if(addendColumnIndex >= 0)
                {
                    if (!ValueCertain(addends[i][addendColumnIndex]))
                        return false;
                }
            }

            if (!ValueCertain(result[column]))
                return false;

            return true;
        }

        static bool ValueCertain(char ch)
        {
            Console.WriteLine($"Checking certainty for char: {ch}");
            if (charIntDict[ch].Count == 1)
                Console.WriteLine($"{ch} is {charIntDict[ch][0]}");

            return charIntDict[ch].Count != 1 ? false : true;
        }

        static void ShowMeWhatYouGot()
        {
            Console.WriteLine("\nSo far:");
            foreach (var l in charIntDict)
            {
                List<string> possibleValues = new List<string>();
                
                for (int i = l.Value.Min(); i <= l.Value.Max(); i++)
                {
                    if (l.Value.Contains(i))
                        possibleValues.Add(i.ToString());
                }
                Console.WriteLine($"{l.Key} could be: {string.Concat(possibleValues)}");
            }
        }

        static void TryFillValuesByAddingColumns()
        {
            for (int i = 0; i < addends.Length; i++)
            {
                for (int j = addends[i].Length - 1; j >= 0; j--)
                {
                    if(columnSums[j] != -1)
                    {
                        if (charIntDict[addends[i][j]].Count == 1)
                        {
                            columnSums[j] += (int)charIntDict[addends[i][j]][0];
                            Console.WriteLine($"Column {j} += {charIntDict[addends[i][j]][0]}");
                        }
                        else
                        {
                            columnSums[j] = -1;
                            Console.WriteLine($"Column {j} has unknown(s), ditching attempt to calculate value");
                        }
                    }    
                }
            }

            for (int i = 0; i < maxAddendLength; i++)
            {
                if (columnSums[i] != -1)
                {
                    RemoveEveryValueFromListExcept(charIntDict[result[i]], columnSums[i]);
                    RemoveValueFromEveryListInDictionaryExcept(columnSums[i], charIntDict, result[i]);
                }

                if (charIntDict[result[i]].Count == 1)
                {
                    if(columnSums[i] != -1)
                        Console.WriteLine($"Column {i} = {columnSums[i]}");
                    
                    Console.WriteLine($"Char {result[i]} = {charIntDict[result[i]][0]}\n");
                }
            }
        }

        private static void RemoveEveryValueFromListExcept(List<int> list, int i)
        {
            for (int j = list.Min(); j <= list.Max(); j++)
            {
                if (list.Contains(j))
                {
                    if (j != i)
                    {
                        list.Remove(j);
                    }
                }
            }
        }

        static void SetPotentialValuesForAddendsWithCharsInTheSameColums()
        {
            // In two addend alphametics, there may be columns that have the same letter in both the addends and the result.
            // If such a column is the units column, that letter must be 0. Otherwise, it can either be 0 or 9 (and then there is a carry).
            if (addends.Length == 2)
            {
                for (int i = 0; i < addends.Length -1; i++)
                {
                    for (int j = 0; j < addends[i].Length; j++)
                    {
                        if (addends[i][j] != result[j])
                        {
                            break;
                        }
                        if (addends[i][addends[i].Length - 1] == result[result.Length - 1] && addends[i +1][addends[i].Length - 1] == result[result.Length - 1])
                        {
                            RemoveEveryValueFromListExcept(charIntDict[result[result.Length - 1]], 0);
                            RemoveValueFromEveryListInDictionaryExcept(0, charIntDict, result[result.Length - 1]);
                            break;
                        }
                    }
                }
            }
        }

        static void SetFirstColumTo1IfResultLongerThanMaxAddendsLength()
        {
            if (result.Length > maxAddendLength)
            {
                RemoveEveryValueFromListExcept(charIntDict[result[0]], 1);
                RemoveValueFromEveryListInDictionaryExcept(1, charIntDict, result[0]);
                columnCarrys[0] = RequiresCarry.Yes;
                Console.WriteLine($"\nchar {result[0]} must be {charIntDict[result[0]][0]}\n");
            }
        }

        static void ResetCharIntDictionary()
        {
            charIntDict = new Dictionary<char, List<int>>();
            for (int i = 0; i < addends.Length; i++)
            {
                for (int j = 0; j < addends[i].Length; j++)
                {
                    if (!charIntDict.ContainsKey(addends[i][j]))
                    {
                        charIntDict.Add(addends[i][j], new List<int>() { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 });
                        Console.WriteLine($"Added {addends[i][j]} to char int dictionary");
                    }
                }
            }

            for (int i = 0; i < result.Length; i++)
            {
                if (!charIntDict.ContainsKey(result[i]))
                {
                    charIntDict.Add(result[i], new List<int>() { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 });
                    Console.WriteLine($"Added {result[i]} to char int dictionary");
                }
            }
        }

        static void FormatStrings(string s)
        {
            string addendStr = s.Substring(0, s.IndexOf('='));
            result = new string(s.Substring(s.IndexOf('='), s.Length - s.IndexOf('=')).Where(c => char.IsLetter(c)).ToArray());

            addends = addendStr.Split('+');
            maxAddendLength = 2;
            for (int i = 0; i < addends.Length; i++)
            {
                addends[i] = new string(addends[i].Where(c => char.IsLetter(c)).ToArray());

                if (addends[i].Length > maxAddendLength) maxAddendLength = addends[i].Length;
            }
            addends = addends.OrderBy(x => x.Length).ToArray();
        }

        static void CreateColumnSumDictionary()
        {
            columnSums = new Dictionary<int, int>();
            for (int i = 0; i < result.Length; i++)
            {
                columnSums.Add(i, 0);
            }    
        }

        static void CreateColumnCarrysList()
        {
            columnCarrys = new List<RequiresCarry>();
            for (int i = 0; i < result.Length; i++)
            {
                columnCarrys.Add(RequiresCarry.NotSure);
            }
        }

        static void CreateColumnSolvedList()
        {
            columSolved = new List<bool>();
            for (int i = 0; i < result.Length; i++)
            {
                columSolved.Add(false);
            }
        }

        static void OptionallyWriteStringsToConsole()
        {
            Console.WriteLine("\nAddends:");
            foreach (var str in addends)
            {
                Console.WriteLine(str);
            }
            Console.WriteLine("\nSum:");
            Console.WriteLine($"{result}\n");
        }
    }
}
