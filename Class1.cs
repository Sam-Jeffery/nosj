using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace nosj
{
    public class NosjFile
    {
        string filePath;

        List<string> fileLines;
        List<string> fileLinesR = new List<string>();


        public NosjFile(string filePath)
        {
            this.filePath = filePath;

            string directoryPath = Path.GetDirectoryName(filePath);

            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            if (!File.Exists(filePath))
            {
                File.Create(filePath).Close();
                // Write an open bracket and a close bracket to the file to initialise
                File.WriteAllText(filePath, "{\n}");
            }

            fileLines = File.ReadAllLines(filePath).ToList();

            //Array.Copy(fileLines, fileLinesR, fileLines.Count);

            fileLines.ForEach((item) =>
            {
                fileLinesR.Add(item);
            });

            int count = 0;
            foreach (string line in fileLinesR.ToList())
            {
                fileLinesR[count] = Regex.Replace(line, "\t", "");
                count++;
            }
        }

        public string ReadString(string path, string key)
        {
            int level = -1;

            string[] pathList = path.Split('/');
            Stack<string> pathed = new Stack<string>();

            string previousLine = null;

            foreach (string line in fileLinesR)
            {
                if (line == "{") // Keep track of how many categories we have entered
                {
                    level++;

                    if (!String.IsNullOrEmpty(previousLine))
                    {
                        string temp = previousLine.Replace("[", "");
                        temp = temp.Replace("]", "");
                        pathed.Push(temp);  // Add current category to stack of categories we have entered
                    }
                }
                else if (line == "}") // Again
                {
                    level--;
                    if (pathed.Count > 0)
                    {
                        pathed.Pop();   // Remove the highest level category from list
                    }
                }

                if (pathed.Count > 0)
                {
                    string[] temp = pathed.ToArray();
                    Array.Reverse(temp);

                    if (Enumerable.SequenceEqual(temp, pathList))    // If we have followed the full path to the right category
                    {
                        if (line.Split(':')[0] == key)
                        {
                            string unformatted = line.Split(':')[1];
                            unformatted = unformatted.Replace("$c", ":");
                            unformatted = unformatted.Replace("$t", "\t");

                            if (unformatted[0] == '(')
                            {
                                return unformatted;
                            }

                            unformatted = unformatted.Replace("$b", "(");
                            unformatted = unformatted.Replace("$n", ")");

                            return unformatted;
                        }
                    }
                }

                previousLine = line;
            }

            return null;
        }

        public string Read(string path, string key)
        {
            return ReadString(path, key);
        }

        public string[] ReadArray(string path, string key)
        {
            string rawString = ReadString(path, key);
            rawString = rawString.Substring(2);
            rawString = rawString.Remove(rawString.Length - 1);
            string[] processedString = rawString.Split(',');
            return processedString;
        }

        public void Write(string path, string key, string value)
        {
            WriteString(path, key, value, false);
        }

        public void Write(string path, string key, string[] values)
        {
            string fstr = "";

            fstr += " (";

            foreach (string item in values)
            {
                string tmp = item.Replace(",", "$p");
                tmp = tmp.Replace("(", "$b");
                tmp = tmp.Replace(")", "$n");
                fstr += tmp + ",";
            }

            fstr = fstr.Remove(fstr.Length - 1);

            fstr += ")";

            WriteString(path, key, fstr, true);
        }

        private void WriteString(string path, string key, string value, bool arr)
        {
            int level = -1;

            string[] pathList = path.Split('/');
            Stack<string> pathed = new Stack<string>();

            string previousLine = null;

            value = value.Replace(":", "$c");
            value = value.Replace("\t", "$t");
            if (!arr)
            {
                value = value.Replace("(", "$b");
                value = value.Replace(")", "$n");
            }

            int i = 0;

            foreach (string line in fileLinesR.ToList())
            {

                if (line == "{") // Keep track of how many categories we have entered
                {
                    level++;

                    if (!String.IsNullOrEmpty(previousLine))
                    {
                        string temp = previousLine.Replace("[", "");
                        temp = temp.Replace("]", "");
                        pathed.Push(temp);  // Add current category to stack of categories we have entered
                    }
                }

                if (pathed.Count > 0)
                {
                    string[] tempArr = pathed.ToArray();
                    Array.Reverse(tempArr);

                    if (Enumerable.SequenceEqual(tempArr, pathList))    // If we have followed the full path to the right category
                    {
                        if (line.Split(':')[0] == key)  // If the value is already stored
                        {
                            string temp = "";
                            foreach (string item in pathed)
                            {
                                temp += "\t";
                            }
                            temp += "\t" + key + ":" + value;

                            fileLines[i] = temp;    // Overwrite it with the modified value

                            string temp2 = Regex.Replace(temp, "\t", "");
                            fileLinesR[i] = temp2;

                            File.WriteAllLines(filePath, fileLines);
                            return; // Stop looping
                        }

                        if (line == "}") // If we have reached the end of the category and still not found the key then we need to add it to a new line
                        {
                            string temp = "";
                            foreach (string item in pathed)
                            {
                                temp += "\t";
                            }
                            temp += "\t" + key + ":" + value;

                            fileLines.Insert(i, temp);  // Add the new key and value to lines list in the correct place

                            string temp2 = Regex.Replace(temp, "\t", "");
                            fileLinesR.Insert(i, temp2);

                            File.WriteAllLines(filePath, fileLines);
                            return; // Stop looping
                        }
                    }
                }

                if (line == "}") // Again
                {
                    int count = 0;
                    bool correctPath = true;

                    string[] tempArr = pathed.ToArray();    // Stacks in c# have newest added at the start so we need to reverse it to be in the same sequence as the pathList array
                    Array.Reverse(tempArr);

                    if (Enumerable.SequenceEqual(tempArr, pathList)) // This shouldnt happen, as the full correct path already exists and the key should already have been found/created and the loop exited.
                    {
                        previousLine = line;
                        i++;
                        continue;
                    }

                    if (pathed.Count < pathList.Length)
                    {
                        foreach (string item in tempArr.Reverse()) // For every category we have entered in reverse order so lowest level categories have highest priority
                        {
                            if (pathList[count] == item)    // Check if it is on the correct path list
                            {
                                correctPath = true; // Record that we are on the correct path
                            }
                            else
                            {
                                correctPath = false;
                            }

                            count++;
                        }
                    }
                    else
                    {
                        correctPath = false;
                    }

                    if (correctPath)    // If we are on the correct path but have not made it to the final category (Because it doesnt yet exist)
                    {
                        string tempconst = "";
                        foreach (string item in tempArr)
                        {
                            tempconst += item + " ";
                        }

                        string tempconst2 = "";
                        foreach (string item in pathList)
                        {
                            tempconst2 += item + " ";
                        }
                        //MessageBox.Show("Correct path but doesnt exist\n" + "Pathed: " + tempconst + "\nFull path: " + tempconst2);

                        // Create category

                        List<string> notPathed = new List<string>();

                        int count2 = pathed.Count;

                        string dbg = "";

                        for (int j = pathed.Count; j < pathList.Count(); j++)    // Add all of the categories we have not yet pathed to an array
                        {
                            notPathed.Add(pathList[j]);
                            dbg += pathList[j];
                        }

                        int lostcount = 0;
                        int addedLines = 0;
                        foreach (string item in notPathed)   // Loop through every item not pathed
                        {
                            string temp = "";
                            foreach (string item2 in pathed)
                            {
                                temp += "\t";
                            }
                            temp += "\t" + "[" + notPathed[lostcount] + "]";    // Add a line with the category
                            fileLines.Insert(i + addedLines, temp);
                            fileLinesR.Insert(i + addedLines, "[" + notPathed[lostcount] + "]");
                            addedLines++;

                            string temp2 = "";
                            foreach (string item2 in pathed)
                            {
                                temp2 += "\t";
                            }
                            temp2 += "\t" + "{";    // Add a line with the open curly brackets
                            fileLines.Insert(i + addedLines, temp2);
                            fileLinesR.Insert(i + addedLines, "{");
                            addedLines++;    // Keep track of how many lines we have added, not including the close curly brackets

                            string temp3 = "";
                            foreach (string item2 in pathed)
                            {
                                temp3 += "\t";
                            }
                            temp3 += "\t" + "}";
                            fileLines.Insert(i + addedLines, temp3); // Add a line with the close curly brackets
                            fileLinesR.Insert(i + addedLines, "}");

                            pathed.Push(item);
                            lostcount++;
                            addedLines--;
                            i++;
                        }

                        string tmpf = "";
                        foreach (string item in pathed)
                        {
                            tmpf += "\t";
                        }
                        tmpf += "\t" + key + ":" + value;

                        fileLines.Insert(i + addedLines, tmpf);  // Add the new key and value to lines list in the correct place
                        fileLinesR.Insert(i + addedLines, key + ":" + value);

                        File.WriteAllLines(filePath, fileLines);

                        string tmp = "";
                        foreach (string line2 in fileLines)
                        {
                            tmp += line2 + "\n";
                        }
                        //MessageBox.Show(tmp);

                        return; // Stop looping
                    }

                    level--;
                    if (pathed.Count > 0)
                    {
                        pathed.Pop();   // Remove the highest level category from list
                    }
                }

                previousLine = line;
                i++;
            }
        }
    }
}