using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace obfuscator
{
    class MySortedList : IComparer<string>
    {
        public int Compare(string x, string y)
        {
            if (x == null || y == null) return Comparer<string>.Default.Compare(x, y);
            if (x.Length == y.Length) return Comparer<string>.Default.Compare(x, y);
            return x.Length > y.Length ? -1 : 1;
        }
    }

    class Program
    {
        
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct OpenFileName
        {
            public int lStructSize;
            public IntPtr hwndOwner;
            public IntPtr hInstance;
            public string lpstrFilter;
            public string lpstrCustomFilter;
            public int nMaxCustFilter;
            public int nFilterIndex;
            public string lpstrFile;
            public int nMaxFile;
            public string lpstrFileTitle;
            public int nMaxFileTitle;
            public string lpstrInitialDir;
            public string lpstrTitle;
            public int Flags;
            public short nFileOffset;
            public short nFileExtension;
            public string lpstrDefExt;
            public IntPtr lCustData;
            public IntPtr lpfnHook;
            public string lpTemplateName;
            public IntPtr pvReserved;
            public int dwReserved;
            public int flagsEx;
        }

        [DllImport("comdlg32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool GetOpenFileName(ref OpenFileName ofn);

        private static string ShowDialog()
        {
            var ofn = new OpenFileName();
            ofn.lStructSize = Marshal.SizeOf(ofn);
            ofn.lpstrFilter = "";
            ofn.lpstrFile = new string(new char[256]);
            ofn.nMaxFile = ofn.lpstrFile.Length;
            ofn.lpstrFileTitle = new string(new char[64]);
            ofn.nMaxFileTitle = ofn.lpstrFileTitle.Length;
            ofn.lpstrTitle = "Откройте текстовый файл для обфускации ";
            if (GetOpenFileName(ref ofn))
                return ofn.lpstrFile;
            return string.Empty;
        }

        static void Main(string[] args)
        {

            Console.WriteLine("Готовимся колдовать :)\n");

            Dictionary<char, string> encodingDictionary = EncodingDictionary();

            Console.WriteLine("Открываем диалог открытия файла...\n");

            string filename = ShowDialog();

            if (filename == string.Empty)
            {
                Console.WriteLine("Не удалось получить путь до файла :(\n\n\n");
                return;
            } else {
                Console.WriteLine("Получен путь до файла: " + filename + "\n");
            }

            Console.WriteLine("Начинаем колдовать...\n");

            StreamReader sr = new StreamReader(filename);

            string tempfilename = Path.GetDirectoryName(filename) + "/temp_" + Path.GetFileName(filename);

            File.Create(tempfilename).Close();

            StreamWriter sw = new StreamWriter(tempfilename, true);

            int num = 0;
            long gotoindex = new Random().Next(1000000000, 2000000000);
            SortedList<string, string> varNamesList = new SortedList<string, string>(new MySortedList());

            bool magic = true;
            string[] AllNamesStringRus = GetAllNamesStringRus().Split(",");
            string[] AllNamesStringEng = GetAllNamesStringEng().Split(",");

            bool methodStarted = false;
            int bracketMultiplier = 0;
            bool bracketEndeddNow = false;
            bool methodStartedNow = false;
            bool methodEndedNow = false;

            int ifMultiplier = 0;
            int doesMultiplier = 0;
            bool ifDoesEndedNow = false;

            char lastSymb = ' ';

            bool needMethodName = false;

            for (string line = sr.ReadLine(); line != null; line = sr.ReadLine())
            {
                num++;
                string gotoindexstring = gotoindex.ToString() + gotoindex.ToString() + gotoindex.ToString() + gotoindex.ToString() + gotoindex.ToString();


                //Console.WriteLine("Строка " + num + ":");
                //Console.WriteLine(line);
                Console.WriteLine("Читаем строку " + num);


                string str = " " + line.Split("//")[0] + " ";
                str = str.Replace("\t", " ").Replace(";", " ; ");

                if (str.Replace(" ", "") == String.Empty || StringContains(str, GetRegionString().Split(","))) continue;

                if (methodEndedNow) methodEndedNow = false;
                if (bracketEndeddNow) bracketEndeddNow = false;
                if (methodStartedNow) methodStartedNow = false;
                if (ifDoesEndedNow) ifDoesEndedNow = false;

                if (StringContains(str, GetIfsStartString().Split(","))) ifMultiplier += 1;
                if (StringContains(str, GetIfsEndString().Split(","))) { ifMultiplier -= 1; if (ifMultiplier == 0) ifDoesEndedNow = true; };

                if (StringContains(str, GetDoesStartString().Split(","))) doesMultiplier += 1;
                if (StringContains(str, GetDoesEndString().Split(","))) { doesMultiplier -= 1; if (doesMultiplier == 0) ifDoesEndedNow = true; };

                if (StringContains(str, GetMethodStartString().Split(","))) { methodStarted = true; methodStartedNow = true; }
                if (StringContains(str, GetMethodEndString().Split(","))) methodStarted = false;

                if (StringContains(str, "экспорт,export".Split(","))) methodEndedNow = true;

                bool bracketWasntZero = bracketMultiplier != 0;
                bracketMultiplier += str.ToCharArray().Count(x => x == '(') - str.ToCharArray().Count(x => x == ')');
                if (bracketWasntZero && bracketMultiplier == 0) bracketEndeddNow = true;

                if (str.Contains("|") || StringContains(str, GetInstrString().Split(","))) { sw.WriteLine(); sw.Write(" "); };

                for (int i = 0; i < AllNamesStringRus.Length; i++)
                {
                    if (magic)
                    {
                        str = Regex.Replace(str, " " + AllNamesStringRus[i] + " ", " " + AllNamesStringEng[i] + " ", RegexOptions.IgnoreCase);
                    }
                    else
                    {
                        str = Regex.Replace(str, " " + AllNamesStringEng[i] + " ", " " + AllNamesStringRus[i] + " ", RegexOptions.IgnoreCase);
                    }
                }

                magic = !magic;
                string newString = String.Empty;
                if (!str.Contains("|") && lastSymb == ';' && !ifDoesEndedNow && ifMultiplier == 0 && doesMultiplier == 0 && !methodStartedNow && methodStarted && bracketMultiplier == 0 && !bracketEndeddNow && !methodEndedNow)
                {
                    if (magic) newString += "goto ~"; else newString += "перейти ~";
                    newString += (gotoindexstring) + "; ";

                    newString += "~" + (gotoindexstring) + ": ";
                    gotoindex = gotoindex + 42;
                }

                if (!str.Contains("|")) str = str.Replace(str.Split('"')[0], str.Split('"')[0]);


                string actString = line.Split("//")[0].Split("|")[0].Split("\"")[0].Replace("\t", " ");

                if (!(actString.Replace(" ", "") == String.Empty))
                {

                    List<string> varNames = new List<string>();

                    if (!needMethodName && StringContains(actString, GetMethodStartString().Split(",")))
                    {
                        needMethodName = true;
                    }

                    if (needMethodName && actString.Contains("("))
                    {
                        string[] subArray = actString.Split('(')[0].Split(" ");
                        varNames.Add(subArray[subArray.Length - 1]);
                        needMethodName = false;
                    }

                    if (StringContains(actString, (GetAdsString()+",=").Split(",")))
                    {
                        bool takeNext = false;
                        bool nextNew = false;
                        foreach (string subString in actString.Replace("\t","").Replace("(", " ( ").Replace("=", " = ").Split(" ", StringSplitOptions.RemoveEmptyEntries))
                        {
                            if (nextNew) { nextNew = false; continue; }
                            if (!takeNext && StringContains(subString.Replace("="," = "), "New,Новый".Split(","))) nextNew = true;
                            if (!takeNext && StringContains(subString, GetAdsString().Split("=")[0].Split(","))) takeNext = true;
                            else if (takeNext && !subString.Contains('.')) { takeNext = false; varNames.Add(subString); }
                        }

                    }

                    foreach (string varName in varNames)
                    {
                       string refVarName = ReplaceUselessSym(varName);
                        if (refVarName == String.Empty || StringContains(refVarName, (GetExceptionsString() + "," + GetAllNamesString()).Split(","))) continue;
                        if (!varNamesList.ContainsKey(refVarName.ToLower())) varNamesList.Add(refVarName.ToLower(), "_"+EncodeString(refVarName, encodingDictionary));
                    }
                }

                sw.Write(newString + str);
                if (StringContains(str, GetInstrString().Split(","))) { sw.WriteLine(); sw.Write(" "); };
                // newString += str;
                lastSymb = str.Replace(" ", "").Last();

            }

            sr.Close();
            sw.Close();

            Console.WriteLine("\nА теперь изменим имена переменных и методов!\n");

            sr = new StreamReader(tempfilename);

            string newfilename = Path.GetDirectoryName(filename) + "/obf_" + Path.GetFileName(filename);
            File.Create(newfilename).Close();
           
            sw = new StreamWriter(newfilename, true);

            num = 0;

            for (string line = sr.ReadLine(); line != null; line = sr.ReadLine())
            {
                Console.WriteLine("Изменяем строку " + num);
                num++;

                string newstring = String.Empty;

                if (line.Contains("|") || StringContains(line, GetInstrString().Split(","))) { sw.WriteLine(); sw.Write(" "); };

                bool change = true;

                foreach (string subString in LetSpaceWithUselessSym(line).Replace("|"," | ").Split(" "))
                {
                    if (subString.Contains('"') && subString.Contains('|')) change = !change;
                    newstring += GetSpaceString();
                    if (change && varNamesList.ContainsKey(subString.ToLower())) newstring += varNamesList[subString.ToLower()];
                    else newstring += subString;
                }

                while (newstring.Contains("< ") || newstring.Contains("> "))
                {
                    newstring = newstring.Replace("< ", "<").Replace("> ", ">");
                }    

                sw.Write(newstring);
                if (StringContains(line, GetInstrString().Split(","))) { sw.WriteLine(); sw.Write(" "); };
            }

            sw.Close();
            sr.Close();
            File.Delete(tempfilename);

            Console.WriteLine("\nОперация завершена! Создан файл "+ newfilename+" \n\n\n");

        }

        static bool StringContains(string str, string[] strArray)
        {
            bool quotationMarks= false;
            
            foreach (string subStringMain in str.Replace("\""," \" ").Split(" ", StringSplitOptions.RemoveEmptyEntries))
            {
                foreach (char c in subStringMain.ToCharArray()) if (c == '"') quotationMarks = !quotationMarks;
                
                if (!quotationMarks)
                    foreach (string subString in strArray)
                        if (subStringMain.ToLower() == subString.ToLower()) return true;
            }
            return false;
        }

        static string GetSpaceString()
        {
            return String.Concat(Enumerable.Repeat(" ", (new Random().Next(28,42))));
        }

        static string ReplaceUselessSym(string str)
        {
            string newStr = str;
            foreach (string subString in GetUsselessSym().Split("|"))
            {
                newStr = newStr.Replace(subString,"");
            }
            return newStr;

        }

        static string LetSpaceWithUselessSym(string str)
        {
            string newStr = str;
            foreach (string substring in GetUsselessSym().Split("|"))
            {
                newStr = newStr.Replace(substring, " " + substring + " ");
            }
            return newStr;
        }

        // Заполним словарь для шифрования
        private static Dictionary<char, string> EncodingDictionary()
        {
            Dictionary<char, string> dictionary = new Dictionary<char, string>();

            string str1 = "_йцукенгшщзхъэждлорпавыфячсмитьбюasdqwertyfghjkluiopzxcvbnm9786542310ё";
            string str2 = "_,251,351,451,551,651,751,851,951,123,126,125,124,127,128,129,130,131,132,133,134,135,136,137,138,139,241,242,243,244,245,246,247,248,249,843,842,841,846,847,848,849,976,986,999,228,777,666,333,444,222,111,000,322,784,785,786,787,789,060,001,003,005,008,010,101,202,303,404,505";

            char[] charArray = str1.ToCharArray();
            string[] stringArray = str2.Split(",");

            if (charArray.Length!=stringArray.Length) throw new Exception(ErrorMessageText("#2-1","AddValuesEncodingDictionary"));

            for (int i = 0; i < charArray.Length; i++)
            {
                if (stringArray[i]!="_" && stringArray[i].ToCharArray().Length != 3) throw new Exception(ErrorMessageText("#2-2", "AddValuesEncodingDictionary"));
                dictionary.Add(charArray[i], stringArray[i]);
            }

            return dictionary;

        }

        private static string EncodeString(string str, Dictionary<char, string> encodingDictionary)
        {
           string newStr = String.Empty;

            foreach (char c in (str.ToLower()).ToCharArray()) {
                if (!encodingDictionary.ContainsKey(c)) throw new Exception(ErrorMessageText("#1-1","EncodeString"));
                newStr = newStr + encodingDictionary[c];
            }

            return newStr;
        }

        static Dictionary<string,string> GetEncodeDictionary(string stringKeys,string stringValues,Dictionary<string, string> dictionary = null)
        {
            if (dictionary == null)
            {
                dictionary = new Dictionary<string, string>();
            }

            string[] keys   = stringKeys.Split(",");
            string[] values = stringValues.Split(",");

            for (int i = 0; i < keys.Length; i++)
            {
                dictionary.Add(keys[i], values[i]);
            }

            return dictionary;
        }

        static string GetAllNamesString() { return GetDoesString() + "," + GetIfsString() + "," + GetInstrString() 
                + "," + GetLogString() + "," + GetTryString() + "," + GetTransactionString() + "," + GetMethodString()
                + "," + GetAdsString(); }
        static string GetAllNamesStringRus() { return GetDoesStringRus() + "," + GetIfsStringRus() + "," + GetInstrStringRus()
                + "," + GetLogStringRus() + "," + GetTryStringRus() + "," + GetTransactionStringRus() + "," + GetMethodStringRus()
                + "," + GetAdsStringRus();}
        static string GetAllNamesStringEng() { return GetDoesStringEng() + "," + GetIfsStringEng() + "," + GetInstrStringEng()
                + "," + GetLogStringEng() + "," + GetTryStringEng() + "," + GetTransactionStringEng() + "," + GetMethodStringEng()
                + "," + GetAdsStringEng();}
        static string GetDoesString() { return GetDoesStringRus() +","+ GetDoesStringEng(); }
        static string GetDoesStringRus() { return GetDoesStartStringRus()+ ",каждого,конеццикла," + GetDoesEndStringRus().ToLower(); }
        static string GetDoesStringEng() { return GetDoesStartStringEng() + ",each,enddo," + GetDoesEndStringEng().ToLower(); }
        static string GetDoesStartString() { return GetDoesStartStringRus()+","+ GetDoesStartStringEng().ToLower(); }
        static string GetDoesStartStringRus() { return "для,пока".ToLower(); }
        static string GetDoesStartStringEng() { return "for,while".ToLower(); }
        static string GetDoesEndString() { return GetDoesEndStringRus()+","+ GetDoesEndStringEng().ToLower(); }
        static string GetDoesEndStringRus() { return "цикл".ToLower(); }
        static string GetDoesEndStringEng() { return "do".ToLower(); }


        static string GetIfsString() { return GetIfsStringRus() + "," + GetIfsStringEng(); }
        static string GetIfsStringRus() { return ""+ GetIfsStartStringRus ()+","+ GetIfsEndStringRus()+ ",конецесли,иначе".ToLower(); }
        static string GetIfsStringEng() { return ""+ GetIfsStartStringEng() + ","+ GetIfsEndStringEng() + ",endif,else".ToLower(); }
        static string GetIfsStartString() { return GetIfsStartStringRus() + "," + GetIfsStartStringEng().ToLower(); }
        static string GetIfsStartStringRus() { return "иначеесли,если".ToLower(); }
        static string GetIfsStartStringEng() { return "elsif,if".ToLower(); }
        static string GetIfsEndString() { return GetIfsEndStringRus() + "," + GetIfsEndStringEng().ToLower(); }
        static string GetIfsEndStringRus() { return "тогда".ToLower(); }
        static string GetIfsEndStringEng() { return "then".ToLower(); }

        static string GetInstrString() { return GetInstrStringRus() + "," + GetInstrStringEng(); }
        static string GetInstrStringRus() { return "#если,#иначеесли,#иначе,#конецесли,клиент,сервер,МобильноеПриложениеКлиент,МобильноеПриложениеСервер,ТолстыйКлиентОбычноеПриложение,ТолстыйКлиентУправляемоеПриложение,ВнешнееСоединение,ТонкийКлиент,ВебКлиент,&НаКлиенте,&НаСервере,&НаСервереБезКонтекста,&НаКлиентеНаСервереБезКонтекста".ToLower(); }
        static string GetInstrStringEng() { return "#if,#elsif,#else,#endif,Client,Server,MobileAppClient,MobileAppServer,ThickClientOrdinaryApplication,ThickClientManagedApplication,ExternalConnection,ThinClient,WebClient,&AtClient,&AtServer,&AtServerNoContext,&AtClientAtServerNoContext".ToLower(); }

        static string GetRegionString() { return GetRegionStringRus() + "," + GetRegionStringEnd(); }
        static string GetRegionStringRus() { return "#область,#конецобласти"; }
        static string GetRegionStringEnd() { return "#Region,#EndRegion"; }

        static string GetLogString() { return GetLogStringRus() + "," + GetLogStringEng(); }
        static string GetLogStringRus() { return " и,или,не,Продолжить,прервать,Возврат,Экспорт,ВызватьИсключение".ToLower(); }
        static string GetLogStringEng() { return " and,or,not,continue,Break,Return,Export,Raise".ToLower(); }

        static string GetAdsString() { return GetAdsStringRus() + "," + GetAdsStringEng(); }
        static string GetAdsStringRus() { return "Знач,Новый,Перем".ToLower(); }
        static string GetAdsStringEng() { return "Val,New,Var".ToLower(); }

        static string GetTryString() { return GetTryStringRus() + "," + GetTryStringEng(); }
        static string GetTryStringRus() { return "Попытка,Исключение,КонецПопытки".ToLower(); }
        static string GetTryStringEng() { return "Try,Except,EndTry".ToLower(); }

        static string GetTransactionString() { return GetTransactionStringRus() + "," + GetTransactionStringEng(); }
        static string GetTransactionStringRus() { return "НачатьТранзакцию,ОтменитьТранзакцию,ЗафиксироватьТранзакцию,ТранзакцияАктивна".ToLower(); }
        static string GetTransactionStringEng() { return "BeginTransaction,RollbackTransaction,CommitTransaction,TransactionActive".ToLower(); }

        static string GetMethodString() { return GetMethodStringRus() + "," + GetMethodStringEng(); }
        static string GetMethodStringRus() { return GetMethodStartStringRus()+","+ GetMethodEndStringRus().ToLower(); }
        static string GetMethodStringEng() { return GetMethodStartStringEng()+ ","+ GetMethodEndStringEng().ToLower(); }
        static string GetMethodStartString() { return GetMethodStartStringRus() + "," + GetMethodStartStringEng().ToLower(); }
        static string GetMethodStartStringRus() { return "Функция,Процедура".ToLower(); }
        static string GetMethodStartStringEng() { return "Function,Procedure".ToLower(); }
        static string GetMethodEndString() { return GetMethodEndStringRus() + "," + GetMethodEndStringEng().ToLower(); }
        static string GetMethodEndStringRus() { return "КонецФункции,КонецПроцедуры".ToLower(); }
        static string GetMethodEndStringEng() { return "EndFunction,EndProcedure".ToLower(); }

        static string GetExceptionsString() { return "неопределено,этотобъект,объект,нстр,встр,ссылка,объект,массив,array,реквизиты".ToLower(); }

        static string GetUsselessSym()
        {
            return " |[|]|(|)|,|.|<|>|=|+|-|;|?|!|'|\"";
        }

        static string ErrorMessageText(string num, string funName)
        {
            return $"\n\n{num} Ошибка в методе {funName}\n\n";
        }

        
    }
}

///////////////////////// Ошибки
// 1-1 ошибка декодирование. Не найден символ в криптографическом справочнике 
// 2-1 ошибка формирования криптографического справочника. Различается количество элементов в массивах
// 2-2 ошибка формирования криптографического справочника. Длина элемента должна быть строго равна 3
