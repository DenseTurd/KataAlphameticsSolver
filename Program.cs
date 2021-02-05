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
     * If not expecting to carry and addend sum > 9 You know somethings wrong
     * We need a revert to last char we're not certain of ting
    */

    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine(Alphametics("SEND + MORE = MONEY"));
            //Console.WriteLine(Alphametics("AD + BA = CE"));
            //Console.WriteLine(Alphametics("MA + MA = ABB")); //"MA + MA = ABB" >> 61 + 61 = 122
        }

        static Dictionary<char, List<int>> mainDict;
        static Dictionary<char, List<int>> testingDict;
        static Dictionary<char, List<int>> charRevertDict;
        static List<Dictionary<char, List<int>>> columnRevertDicts;
        static int testColumn;
        static string[] addends;
        static string result;
        static List<int> remainingValues = new List<int>() { 9, 8, 7, 6, 5, 4, 3, 2, 1, 0 };
        static Dictionary<int, int> columnSums;
        static int maxAddendLength;
        
        static List<bool> columnCarrys;
        static List<bool> testCarrys;
        static List<bool> columSolved;
        static char lastCharWeGuessedAValueFor;

        public static string Alphametics(string s)
        { 
            FormatStrings(s);
            CreateColumnCarrysList();
            CreateColumnSumDictionary();
            CreateColumnSolvedList();
            ResetCharIntDictionarys();

            OptionallyWriteStringsToConsole();

            NoLeadingZeroes();

            SetFirstColumTo1IfResultLongerThanMaxAddendsLength();

            SetPotentialValuesForAddendsWithCharsInTheSameColums();

            TryFillValuesByAddingColumns();

            SystematicValueTestingAndAssigning();

            var outStr = testingDict.Aggregate(s, (current, value) => current.Replace(value.Key, value.Value[0].ToString().ToCharArray()[0]));
            return "\nMy best guess: " + outStr;
        }

        static void NoLeadingZeroes()
        {
            for (int i = 0; i < addends.Length; i++)
            {
                mainDict[addends[i][0]].Remove(0);
            }

            mainDict[result[0]].Remove(0);
            Console.WriteLine("\nNo leading zeroes\n");
        }

        static void SystematicValueTestingAndAssigning()
        {
            Console.WriteLine("\nStarting algorithm\n");
            CopyMainDictionaryToTestingDictionary();
            CreateRevertDictionarys();
            CopyColumnCarrysToTestCarrys();
            testColumn = 0;
            while (testColumn < result.Length)
            {
                if (!AllValuesKnown(testColumn))
                {
                    Console.WriteLine($"There are unknown values in column {testColumn}\n");
                    ConstrainColumn(testColumn);
                    if (!columSolved[testColumn])
                    {
                        CopyTestingDictionaryToColumnRevertDictionary(testColumn);
                        TryValuesForColumn(testColumn);
                    }
                }
                testColumn++;
                ShowMeWhatYouGot();
            }

            ShowMeWhatYouGot();
        }

        static void TryValuesForColumn(int column)
        {
            CreateCharRevertDictionary();
            int addendSum = 0;
            for (int i = 0; i < addends.Length; i++)
            {
                int globalIndex = (addends[i].Length - result.Length) + column;
                if (globalIndex >= 0)
                {
                    char currentChar = addends[i][globalIndex];
                    if (testingDict[currentChar].Count != 1)
                    {
                        Console.WriteLine($"{currentChar}");
                        GetAndSetMinPossibleVal(testingDict, currentChar);
                        lastCharWeGuessedAValueFor = currentChar;
                        Console.WriteLine($"Setting char {currentChar} to {testingDict[currentChar][0]} for now");
                    }
                    RemoveValueFromEveryListInDictionaryExcept(testingDict[currentChar][0], testingDict, currentChar);
                    addendSum += testingDict[currentChar][0];
                    Console.WriteLine($"addend sum currently: {addendSum}");
                }
            }
            Console.WriteLine($"The addend sum for column {column} is: {addendSum}. Testing against result");
            CheckPreviousColumnCarryRequirement(addendSum, column);
        }

        static void CheckPreviousColumnCarryRequirement(int addendSum, int column)
        {
            if (column -1 >= 0)
            {
                if (testCarrys[column - 1] == true)
                {
                    if (addendSum > 9)
                    {
                        int addendSumMinusCarry = addendSum % 10;
                        int carryValue = (addendSum - addendSumMinusCarry) / 10; 
                        Console.WriteLine($"Carried from column {column} to column {column -1}, new addend sum to test is {addendSumMinusCarry}");
                        CheckAddendSumAgainstResult(addendSumMinusCarry, column);
                    }
                    else
                    {
                        if(testCarrys[column] == true)
                        {
                            if(addendSum + 1 > 9) // will need like a max carry value or something
                            {
                                Console.WriteLine($"addend sum plus 1 is larger than 9 (this column ({column}) requires carry so thats good)");
                                int addendSumMinusCarry = addendSum % 10;
                                int carryValue = (addendSum - addendSumMinusCarry) / 10;
                                CheckAddendSumAgainstResult(addendSumMinusCarry, column);
                            }
                            else
                            {
                                Console.WriteLine($"Addend sum not large enough when combine with carry");
                                OhWellTryAgain(column);
                            }
                        }
                        else
                        {
                            Console.WriteLine($"Column {column} needs to carry into column {column - 1}, current addendSum is too small");
                            OhWellTryAgain(column);
                        }
                    }
                }
                else // if previous column doesn't require a carry
                {
                    if (addendSum > 9)
                    {
                        Console.WriteLine($"Addend sum for column {column} is larger than 9 but column {column - 1} does not require a carry. Need to retry previous column");
                        columSolved[column - 1] = false;
                        // revert to last column make something different and start testing again
                    }
                    else
                    {
                        CheckAddendSumAgainstResult(addendSum, column);
                    }
                }
            }
            else // if its column 0
            {
                CheckAddendSumAgainstResult(addendSum, column);
            }
        }

        static void CheckAddendSumAgainstResult(int addendSum, int column)
        {
            if (testingDict[result[column]].Count == 1) // if we know the result value for this column
            {
                Console.WriteLine($"Column {column} of the result already has a value: {testingDict[result[column]][0]}");
                if (testCarrys[column] == true)
                {
                    if (addendSum + 1 == testingDict[result[column]][0]) // will need to adjust for actual carry value
                    {
                        Console.WriteLine($"The values fit with a carry from the next column");
                        columSolved[column] = true;
                    }
                    else
                    {
                        Console.WriteLine($"The values dont fit with a carry from next colum");
                        OhWellTryAgain(column);
                    }
                }
                else if (testCarrys[column] == false)
                {
                    if (addendSum == testingDict[result[column]][0])
                    {
                        Console.WriteLine($"The values fit with no carry from the next column");
                        columSolved[column] = true;
                    }
                    else
                    {
                        Console.WriteLine($"The values dont fit with no carry from next column");
                        OhWellTryAgain(column);
                    }
                }
            }
            else // if we dont have a result for this column
            {
                Console.WriteLine($"Column {column} of the result does not have a value assigned");
                if (testCarrys[column] == true)
                {
                    addendSum = addendSum + 1; // will need to set for differnt carry values
                    if (addendSum + 1 > 9) addendSum = addendSum % 10;
                    if (addendSum < 0)
                    {
                        Console.WriteLine($"Addend sum + carry value cannot be < 0");
                        OhWellTryAgain(column);
                    }
                    
                    if (testingDict[result[column]].Contains(addendSum)) 
                    {
                        Console.WriteLine($"Setting value to {addendSum} Requiring carry from column {column +1}");

                        SetValForChar(testingDict, addendSum, result[column]);
                        columSolved[column] = true;
                    }
                    else
                    {
                        Console.WriteLine($"{addendSum} is already taken (addend sum + carry)");
                        OhWellTryAgain(column);
                    }
                }
                else
                {
                    if (testingDict[result[column]].Contains(addendSum))
                    {
                        Console.WriteLine($"Setting value to {addendSum} without carry");
                        SetValForChar(testingDict, addendSum, result[column]);
                        columSolved[column] = true;
                    }
                    else
                    {
                        Console.WriteLine($"{addendSum} is already taken");
                        OhWellTryAgain(column);
                    }
                }
            }
        }

        private static void OhWellTryAgain(int column)
        {
            Console.WriteLine($"Trying again");

            RemoveValuesThatWontWorkForChar(testingDict[lastCharWeGuessedAValueFor][0], lastCharWeGuessedAValueFor, column);

            // if there are no values left to try
            if (testingDict[lastCharWeGuessedAValueFor].Count == 0) // will need adjusting for more than 2 addends
            {
                if (testCarrys[column] == false && column != result.Length - 1)
                {
                    Console.WriteLine($"Ran out of options for char {lastCharWeGuessedAValueFor} trying colum again with a carry required");
                    CopyColumnRevertDictionaryToTestingDictionary(column);
                    testCarrys[column] = true;
                    TryValuesForColumn(column);
                    return;
                }
                Console.WriteLine($"Ran out of options for char {lastCharWeGuessedAValueFor} redoing last column with a carry this time");
                // reset testing dict to whatever it was when we started working on the previous column
                CopyColumnRevertDictionaryToTestingDictionary(column - 1);
                // tell the previous column to require a carry
                Console.WriteLine($"Setting column {column - 1} to require carry");
                testCarrys[column - 1] = true;
                // start testing from previous column
                testColumn--;
                TryValuesForColumn(testColumn);
                return;
            }

            TryValuesForColumn(column);
        }

        static void RemoveValuesThatWontWorkForChar(int val, char ch, int column)
        {
            CopyCharRevertDictionaryToTestingDictionary();

            for (int i = val; i >= 0; i--)
            {
                if (testingDict[lastCharWeGuessedAValueFor].Contains(i))
                {
                    testingDict[lastCharWeGuessedAValueFor].Remove(i);
                    Console.WriteLine($"Removing {i} from possible values for {lastCharWeGuessedAValueFor}");
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
            var constraintTempDict = testingDict;
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
                        char currentChar = addends[i][0];
                        GetAndSetMinPossibleVal(constraintTempDict, currentChar);
                        Console.WriteLine($"{addends[i][0]} set to {constraintTempDict[currentChar][0]}");
                        columnAddendsSum += constraintTempDict[currentChar][0];
                    }
                }
                SetMinPossibleValFor(mainDict, columnAddendsSum, result[0]);
                CopyMainDictionaryToTestingDictionary();
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
            if (mainDict[ch].Count == 1)
                Console.WriteLine($"{ch} is {mainDict[ch][0]}");

            return mainDict[ch].Count != 1 ? false : true;
        }

        static void ShowMeWhatYouGot()
        {
            Console.WriteLine("\nSo far:");
            foreach (var l in testingDict)
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
                        if (mainDict[addends[i][j]].Count == 1)
                        {
                            columnSums[j] += (int)mainDict[addends[i][j]][0];
                            Console.WriteLine($"Column {j} += {mainDict[addends[i][j]][0]}");
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
                    RemoveEveryValueFromListExcept(mainDict[result[i]], columnSums[i]);
                    RemoveValueFromEveryListInDictionaryExcept(columnSums[i], mainDict, result[i]);
                }

                if (mainDict[result[i]].Count == 1)
                {
                    if(columnSums[i] != -1)
                        Console.WriteLine($"Column {i} = {columnSums[i]}");
                    
                    Console.WriteLine($"Char {result[i]} = {mainDict[result[i]][0]}\n");
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
                            RemoveEveryValueFromListExcept(mainDict[result[result.Length - 1]], 0);
                            RemoveValueFromEveryListInDictionaryExcept(0, mainDict, result[result.Length - 1]);
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
                RemoveEveryValueFromListExcept(mainDict[result[0]], 1);
                RemoveValueFromEveryListInDictionaryExcept(1, mainDict, result[0]);
                columnCarrys[0] = true;
                Console.WriteLine($"\nchar {result[0]} must be {mainDict[result[0]][0]}\n");
            }
        }

        static void CopyMainDictionaryToTestingDictionary()
        {
            testingDict = new Dictionary<char, List<int>>();
            foreach (var keyValPair in mainDict)
            {
                List<int> replacementList = new List<int>();
                for (int i = 0; i < keyValPair.Value.Count; i++)
                {
                    replacementList.Add(keyValPair.Value[i]);
                }
                testingDict.Add(keyValPair.Key, replacementList);
            }
        }

        static void CopyCharRevertDictionaryToTestingDictionary()
        {
            testingDict = new Dictionary<char, List<int>>();
            foreach (var keyValPair in charRevertDict)
            {
                List<int> replacementList = new List<int>();
                for (int i = 0; i < keyValPair.Value.Count; i++)
                {
                    replacementList.Add(keyValPair.Value[i]);
                }
                testingDict.Add(keyValPair.Key, replacementList);
            }
        }

        static void CreateRevertDictionarys()
        {
            columnRevertDicts = new List<Dictionary<char, List<int>>>();
            for (int i = 0; i < result.Length; i++)
            {
                columnRevertDicts.Add(new Dictionary<char, List<int>>());
            }
        }

        static void CreateCharRevertDictionary()
        {
            charRevertDict = new Dictionary<char, List<int>>();
            foreach (var keyValPair in testingDict)
            {
                List<int> replacementList = new List<int>();
                for (int i = 0; i < keyValPair.Value.Count; i++)
                {
                    replacementList.Add(keyValPair.Value[i]);
                }
                charRevertDict.Add(keyValPair.Key, replacementList);
            }
        }

        static void CopyTestingDictionaryToColumnRevertDictionary(int column)
        {
            columnRevertDicts[column] = new Dictionary<char, List<int>>();
            foreach (var keyValPair in testingDict)
            {
                List<int> replacementList = new List<int>();
                for (int i = 0; i < keyValPair.Value.Count; i++)
                {
                    replacementList.Add(keyValPair.Value[i]);
                }
                columnRevertDicts[column].Add(keyValPair.Key, replacementList);
            }
            Console.WriteLine($"Revert dictionary for column {column} created");
        }

        static void CopyColumnRevertDictionaryToTestingDictionary(int column)
        {
            testingDict = new Dictionary<char, List<int>>();
            foreach (var keyValPair in columnRevertDicts[column])
            {
                List<int> replacementList = new List<int>();
                for (int i = 0; i < keyValPair.Value.Count; i++)
                {
                    replacementList.Add(keyValPair.Value[i]);
                }
                testingDict.Add(keyValPair.Key, replacementList);
            }
            Console.WriteLine($"\nReveting to dictionary for column {column}\n");
        }

        static void ResetCharIntDictionarys()
        {
            mainDict = new Dictionary<char, List<int>>();
            for (int i = 0; i < addends.Length; i++)
            {
                for (int j = 0; j < addends[i].Length; j++)
                {
                    if (!mainDict.ContainsKey(addends[i][j]))
                    {
                        mainDict.Add(addends[i][j], new List<int>() { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 });
                        Console.WriteLine($"Added {addends[i][j]} to dictionary");
                    }
                }
            }

            for (int i = 0; i < result.Length; i++)
            {
                if (!mainDict.ContainsKey(result[i]))
                {
                    mainDict.Add(result[i], new List<int>() { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 });
                    Console.WriteLine($"Added {result[i]} to dictionary");
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
            columnCarrys = new List<bool>();
            for (int i = 0; i < result.Length; i++)
            {
                columnCarrys.Add(false);
            }
        }

        static void CopyColumnCarrysToTestCarrys()
        {
            testCarrys = new List<bool>();
            for (int i = 0; i < result.Length; i++)
            {
                testCarrys.Add(columnCarrys[i]);
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
