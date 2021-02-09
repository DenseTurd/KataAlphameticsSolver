using System;
using System.Linq;
using System.Collections.Generic;
using Sprache;

// Errrr.....

public static class Alphametics
{
    public static IDictionary<char, int> Solve(string equation)
    {
        var allAdded = all.Parse(equation);
        var allChars = allAdded.added.Append(allAdded.summed).SelectMany(o => o).Distinct();
        var cantBeZero = allAdded.added.Append(allAdded.summed).Select(o => o.First()).Distinct();

        var result = Combinations(allChars, Enumerable.Empty<int>(), cantBeZero)
            .Select(o => o.ToDictionary(m => m.letter, m => m.value))
            .FirstOrDefault(m => IsValid(allAdded.added, allAdded.summed, m));

        return result ?? throw new ArgumentException();
    }

    private static IEnumerable<IEnumerable<(char letter, int value)>> Combinations(IEnumerable<char> remaining, IEnumerable<int> used, IEnumerable<char> cantBeZero)
    {
        if (!remaining.Any())
        {
            yield return Enumerable.Empty<(char letter, int value)>();
            yield break;
        }

        var letter = remaining.First();
        var start = cantBeZero.Contains(letter) ? 1 : 0;

        foreach (var n in Enumerable.Range(start, 10 - start).Except(used))
        {
            var option = (letter, n);
            foreach (var next in Combinations(remaining.Skip(1), used.Append(n), cantBeZero))
                yield return next.Append(option);
        }
    }

    private static bool IsValid(char[][] added, char[] summed, Dictionary<char, int> mapping)
    {
        long asNumber(char[] chars) =>
            chars.Reverse()
                .Select((c, i) => mapping.GetValueOrDefault(c) * (long)Math.Pow(10, i))
                .Sum();

        var targetNumber = asNumber(summed);
        long sum = 0;
        foreach (var chars in added)
        {
            var number = asNumber(chars);
            if (number > targetNumber || sum + number > targetNumber)
                return false;
            sum += number;
        }
        return sum == targetNumber;
    }

    private static Parser<char[]> added =
        from numbers in Parse.AtLeastOnce(Parse.Letter)
        from _ in Parse.Optional(Parse.WhiteSpace.Then(_ => Parse.Char('+')).Then(_ => Parse.WhiteSpace))
        select numbers.ToArray();

    private static Parser<char[]> summed =
        from _ in Parse.WhiteSpace.Then(_ => Parse.String("==")).Then(_ => Parse.WhiteSpace)
        from numbers in Parse.AtLeastOnce(Parse.Letter)
        select numbers.ToArray();

    private static Parser<(char[][] added, char[] summed)> all =
        from allAdded in Parse.Many(added)
        from result in summed
        select (allAdded.ToArray(), result);
}

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
    */

    class Program
    {
        static void Main(string[] args)
        {
            //Console.WriteLine(Alphametics("ZEROES + ONES = BINARY")); //"ZEROES + ONES = BINARY\" -> \"698392 + 3192 = 701584\"
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
        static Dictionary<int, int> columnSums;
        static int maxAddendLength;
        static List<bool> columnCarrys;
        static List<bool> testCarrys;
        static char lastCharWeGuessedAValueFor;

        public static string Alphametics(string s)
        { 
            FormatStrings(s);
            CreateColumnCarrysList();
            CreateColumnSumDictionary();
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
                if (!AllValuesKnown(testColumn, testingDict))
                {
                    Console.WriteLine($"There are unknown values in column {testColumn}\n");
                    ConstrainColumn(testColumn);
                    if (AllAddendsKnown(testColumn))
                    {
                        CopyTestingDictionaryToColumnRevertDictionary(testColumn);
                        Console.WriteLine($"We know all the addends for column {testColumn}");
                        if (AllValuesKnown(testColumn + 1, testingDict))
                        {
                            if (testCarrys[testColumn] == true)
                            {
                                Console.WriteLine($"We know all the values for the next column and know this one ({testColumn}) requires a carry");
                                if (testingDict[result[testColumn]].Contains(KnownAddendSumMod10(testColumn) + 1))
                                {
                                    Console.WriteLine("Setting result accordingly");
                                    SetValForChar(testingDict, KnownAddendSumMod10(testColumn) + 1, result[testColumn]); // will need to sort for diff carry values
                                }
                                else
                                {
                                    Console.WriteLine($"Nice try, lets go again\n");
                                    CopyColumnRevertDictionaryToTestingDictionary(0);
                                    RemoveLowestUnknownFromColumnMainDict(0);
                                    testColumn = 0;
                                }
                            }
                            else
                            {

                                Console.WriteLine($"We know all the values for the next column and know this one don't require a carry");
                                if (testingDict[result[testColumn]].Contains(KnownAddendSumMod10(testColumn)))
                                {
                                    Console.WriteLine("Setting result accordingly");
                                    SetValForChar(testingDict, KnownAddendSumMod10(testColumn), result[testColumn]); // will need to sort for diff carry values
                                }
                                else
                                {
                                    Console.WriteLine($"Nice try, lets go again\n");
                                    CopyColumnRevertDictionaryToTestingDictionary(0);
                                    RemoveLowestUnknownFromColumnMainDict(0);
                                    testColumn = 0;
                                }
                            }
                        }
                    }
                    else
                    {
                        CopyTestingDictionaryToColumnRevertDictionary(testColumn);
                        TryValuesForColumn(testColumn);
                    }
                }
                else
                {
                    testColumn++;
                    ShowMeWhatYouGot();
                    if (testColumn == result.Length)
                    {
                        Console.WriteLine("\n TRYING SUM");
                        if (!SumWorks())
                        {
                            Console.WriteLine("Didn't work\n");
                            CopyColumnRevertDictionaryToTestingDictionary(0);
                            RemoveLowestUnknownFromColumnMainDict(0);
                            testColumn = 0;
                        }
                    }
                }
            }

            ShowMeWhatYouGot();
        }

        static int KnownAddendSumMod10(int column)
        {
            int addendSum = 0;
            for (int i = 0; i < addends.Length; i++)
            {
                int globalIndex = (addends[i].Length - result.Length) + column;
                if (globalIndex >= 0)
                {
                    char currentChar = addends[i][globalIndex];
                    addendSum += testingDict[currentChar][0];
                }
            }
            Console.WriteLine($"Known addend sum is {addendSum}");
            return addendSum % 10;
        }

        static bool AllAddendsKnown(int column)
        {
            for (int i = 0; i < addends.Length; i++)
            {
                int globalIndex = (addends[i].Length - result.Length) + column;
                if (globalIndex >= 0)
                {
                    char currentChar = addends[i][globalIndex];
                    if (testingDict[currentChar].Count != 1)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        static bool SumWorks()
        {
            List<int> addendsAsInts = new List<int>();
            int resultAsInt;
            for (int i = 0; i < addends.Length; i++)
            {
               addendsAsInts.Add(MakeInt(addends[i]));
            }
            resultAsInt = MakeInt(result);

            int AddendsSum = 0;
            for (int i = 0; i < addendsAsInts.Count; i++)
            {
                AddendsSum += addendsAsInts[i];
                Console.WriteLine($"  {addendsAsInts[i]} +");
            }
            Console.WriteLine($"= {resultAsInt}");

            return AddendsSum == resultAsInt;
        }

        static int MakeInt(string str)
        {
            int inty = 0;
            for (int i = 0; i < str.Length; i++)
            {               
                inty += (int)(testingDict[str[i]][0] * MathF.Pow(10, (str.Length -1) - i));
            }
            return inty;
        }

        static void TryValuesForColumn(int column)
        {
            ShowMeWhatYouGot();
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
            //Console.WriteLine($"The addend sum for column {column} is: {addendSum}. Testing against result");
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
                Console.WriteLine($"Column {column} of the result ({result[column]}) does not have a value assigned");
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
                        Console.WriteLine($"Setting {result[column]} to {addendSum} Requiring carry from column {column +1}");

                        SetValForChar(testingDict, addendSum, result[column]);
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
            Console.WriteLine($"Trying again\n");

            if (!RemoveValuesThatWontWorkForChar(testingDict[lastCharWeGuessedAValueFor][0], lastCharWeGuessedAValueFor, column))
            {
                CopyColumnRevertDictionaryToTestingDictionary(0);
                RemoveLowestUnknownFromColumnMainDict(0);
                testColumn = 0;
                return;
            }


            // if there are no values left to try
            if (testingDict[lastCharWeGuessedAValueFor].Count == 0) // will need adjusting for more than 2 addends
            {
                Console.WriteLine($"Ran out of options for char {lastCharWeGuessedAValueFor}");
                CopyColumnRevertDictionaryToTestingDictionary(column - 1);

                if (ColumnMustRequireCarry(column -1))
                {
                    Console.WriteLine($"Previous column {column -1} must require carry, incrementing unknown in previous column");
                    RemoveLowestUnknownFromColumn(column - 1);
                }
                else
                {
                    Console.WriteLine($"Setting previous column ({column -1}) to not require carry, incrementing unknown in previous column");
                    RemoveLowestUnknownFromColumn(column - 1);
                    testCarrys[column - 1] = false;
                }
                // when we backtrack set the current column back to require carry = true
                testCarrys[column] = column < result.Length-1; // Unless it's the last column
                testColumn--;
                TryValuesForColumn(testColumn);
                return;
                // last column should never be able to get set to require carry
            }
            TryValuesForColumn(column);
        }

        static bool ColumnMustRequireCarry(int column)
        {
            int addendSumWithoutUnknowns = 0;
            char unknown = '*';
            int unknowns = 0;
            for (int i = 0; i < addends.Length; i++)
            {
                int globalIndex = (addends[i].Length - result.Length) + column;
                if (globalIndex >= 0)
                {
                    char currentChar = addends[i][globalIndex];
                    if (testingDict[currentChar].Count != 1)
                    {
                        if (unknown == '*')
                        {
                            unknown = currentChar;
                            unknowns++;
                        } 
                        else
                        {
                            unknowns++;
                        }
                    }
                    else
                    {
                        addendSumWithoutUnknowns += testingDict[currentChar][0];
                    }
                }
            }

            if (unknowns > 1)
            {
                return false;
            }
            if (unknown != result[column] && addendSumWithoutUnknowns == 0)
            {
                return true;
            }
            return false;
        }

        static void RemoveLowestUnknownFromColumn(int column)
        {
            char currentChar;
            for (int i = 0; i < addends.Length; i++)
            {
                int globalIndex = (addends[i].Length - result.Length) + column;
                if (globalIndex >= 0)
                {
                    if (testingDict[addends[i][globalIndex]].Count != 1)
                    {
                        currentChar = addends[i][globalIndex];
                        SetMinPossibleValFor(testingDict, testingDict[currentChar].Min() + 1, currentChar);
                        CopyTestingDictionaryToColumnRevertDictionary(column);
                        return;
                    }
                } 
            }

            if (testingDict[result[column]].Count != 1)
            {
                currentChar = result[column];
                SetMinPossibleValFor(testingDict, testingDict[currentChar].Min() + 1, currentChar);
                CopyTestingDictionaryToColumnRevertDictionary(column);
            }
        }

        static void RemoveLowestUnknownFromColumnMainDict(int column)
        {
            char currentChar;
            for (int i = 0; i < addends.Length; i++)
            {
                int globalIndex = (addends[i].Length - result.Length) + column;
                if (globalIndex >= 0)
                {
                    if (mainDict[addends[i][globalIndex]].Count != 1)
                    {
                        currentChar = addends[i][globalIndex];
                        SetMinPossibleValFor(mainDict, mainDict[currentChar].Min() + 1, currentChar);
                        CopyMainDictionaryToTestingDictionary();
                        CopyTestingDictionaryToColumnRevertDictionary(column);
                        return;
                    }
                }
            }

            if (mainDict[result[column]].Count != 1)
            {
                currentChar = result[column];
                SetMinPossibleValFor(mainDict, mainDict[currentChar].Min() + 1, currentChar);
                CopyMainDictionaryToTestingDictionary();
                CopyTestingDictionaryToColumnRevertDictionary(column);
            }
        }

        static bool RemoveValuesThatWontWorkForChar(int val, char ch, int column)
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
            return testingDict[lastCharWeGuessedAValueFor].Count > 0;
        }

        static void SetValForChar(Dictionary<char, List<int>> dict, int i, char ch)
        {
            RemoveEveryValueFromListExcept(dict[ch], i);
            RemoveValueFromEveryListInDictionaryExcept(i, dict, ch);
        }

        static void ConstrainColumn(int column)
        {
            Console.WriteLine($"Working out constraints for column {column}");
            var constraintTempDict = testingDict;
            if (column == 0)
            {
                Console.WriteLine($"Making sure column {column} of the result cannot be smaller than the its minimum possible addends");
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

            if (column < result.Length - 1)
            {
                if (AllValuesKnown(column + 1, testingDict))   // if we know everything about the next column we can be sure of some things for this column
                {
                    Console.WriteLine($"We know all the values for the next column ({column + 1}) Lets see if it can help us with this one ({column})");
                    int columnAddendsSum = 0;
                    for (int i = 0; i < addends.Length; i++)
                    {
                        int globalIndex = (addends[i].Length - result.Length) + column;
                        if (globalIndex == column + 1)
                        {
                            char currentChar = addends[i][0];
                            columnAddendsSum += testingDict[currentChar][0];
                        }
                    }
                    if (columnAddendsSum > 9)
                    {
                        Console.WriteLine($"The next column ({column + 1}) must carry into this one ({column}), as its addends sum to more than 9");
                        testCarrys[column] = true;
                    }
                }
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

        static bool AllValuesKnown(int column, Dictionary<char, List<int>> dict)
        {
            Console.WriteLine($"\nChecking column: {column}");
            int addendSum = 0;
            for (int i = 0; i < addends.Length; i++)
            {
                int globalIndex = (addends[i].Length - result.Length) + column;
                if(globalIndex >= 0)
                {
                    if (!ValueCertain(addends[i][globalIndex], dict))
                        return false;
                    addendSum += addends[i][globalIndex];
                }
            }

            if (!ValueCertain(result[column], dict))
                return false;

            testCarrys[column] = addendSum % 10 != result[column]; // set carry if we know all values
            return true;
        }

        static bool ValueCertain(char ch, Dictionary<char, List<int>> dict)
        {
            Console.WriteLine($"Checking certainty for char: {ch}");
            if (dict[ch].Count == 1)
                Console.WriteLine($"{ch} is {testingDict[ch][0]}");

            return dict[ch].Count != 1 ? false : true;
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

        static void RemoveEveryValueFromListExcept(List<int> list, int i)
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
                columnCarrys.Add(i < result.Length -1);
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
