using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MySql.Data.MySqlClient;
using System.Text.RegularExpressions;

namespace mediaparse
{
    public static class StackBasedIteration
    {
        static void Main(string[] args)
        {
            // Specify the starting folder on the command line, or in 
            // Visual Studio in the Project > Properties > Debug pane.
            TraverseTree(args[0]);

            Console.WriteLine("Press any key");
            Console.ReadKey();
        }

        public static string MySqlEscape(string usString)
        {
            if (usString == null)
            {
                return null;
            }
            // it escapes \r, \n, \x00, \x1a, baskslash, single quotes, and double quotes
            return Regex.Replace(usString, @"[\r\n\x00\x1a\\'""]", @"\$0");
        }

        public static void addgenres(string genre)
        {
            bool HasRows;
            string MyConString = "SERVER=192.168.5.106;" +
                "DATABASE=jukebox;" +
                "UID=jukebox;" +
                "PASSWORD=;";
            MySqlConnection connection = new MySqlConnection(MyConString);

            connection.Open();
            MySqlCommand command = connection.CreateCommand();
            MySqlDataReader Reader;
           
            command.CommandText = "SELECT uid FROM genres WHERE genre='" + MySqlEscape(genre.Trim()) + "'";
            Reader = command.ExecuteReader();
            HasRows = Reader.HasRows;
            Reader.Close();
            connection.Close();
            if (!HasRows)
            {
                connection.Open();
                string sql = "INSERT INTO genres (genre) VALUES ('" + MySqlEscape(genre.Trim()) + "')";
                command = new MySqlCommand(sql, connection);
                command.ExecuteNonQuery();
                connection.Close();
            }
        }

        public static void TraverseTree(string root)
        {
            string MyConString = "SERVER=192.168.5.106;" +
                "DATABASE=jukebox;" +
                "UID=jukebox;" +
                "PASSWORD=;";
            MySqlConnection connection = new MySqlConnection(MyConString);

            // Data structure to hold names of subfolders to be
            // examined for files.
            Stack<string> dirs = new Stack<string>(20);

            if (!System.IO.Directory.Exists(root))
            {
                throw new ArgumentException();
            }
            dirs.Push(root);

            while (dirs.Count > 0)
            {
                string currentDir = dirs.Pop();
                string[] subDirs;
                try
                {
                    subDirs = System.IO.Directory.GetDirectories(currentDir);
                }
                // An UnauthorizedAccessException exception will be thrown if we do not have
                // discovery permission on a folder or file. It may or may not be acceptable 
                // to ignore the exception and continue enumerating the remaining files and 
                // folders. It is also possible (but unlikely) that a DirectoryNotFound exception 
                // will be raised. This will happen if currentDir has been deleted by
                // another application or thread after our call to Directory.Exists. The 
                // choice of which exceptions to catch depends entirely on the specific task 
                // you are intending to perform and also on how much you know with certainty 
                // about the systems on which this code will run.
                catch (UnauthorizedAccessException e)
                {
                    Console.WriteLine(e.Message);
                    continue;
                }
                catch (System.IO.DirectoryNotFoundException e)
                {
                    Console.WriteLine(e.Message);
                    continue;
                }

                string[] files = null;
                try
                {
                    files = System.IO.Directory.GetFiles(currentDir);
                }

                catch (UnauthorizedAccessException e)
                {

                    Console.WriteLine(e.Message);
                    continue;
                }

                catch (System.IO.DirectoryNotFoundException e)
                {
                    Console.WriteLine(e.Message);
                    continue;
                }
                
                // Perform the required action on each file here.
                // Modify this block to perform your required task.
                foreach (string file in files)
                {
                    try
                    {
                        // Perform whatever action is required in your scenario.
                        System.IO.FileInfo fi = new System.IO.FileInfo(file);
                        Console.WriteLine("{3}\\{0}: {1}, {2}", fi.Name, fi.Length, fi.CreationTime, fi.DirectoryName);

                        if (fi.Name.Contains(".mp3"))
                        {
                            string mp3filepath = file;
                            TagLib.File mp3file = TagLib.File.Create(mp3filepath);
                            string[] mp3fileartists = mp3file.Tag.AlbumArtists;
                            string[] mp3fileperformers = mp3file.Tag.Performers;
                            string mp3filetitle = mp3file.Tag.Title;
                            string[] mp3filegenres = mp3file.Tag.Genres;
                            string genres = "";
                            string artists = "";
                            string performers = "";
                            try
                            {
                                foreach (string artist in mp3fileartists)
                                {
                                    if (artists == "")
                                    {
                                        artists = artist;
                                    }
                                    else
                                    {
                                        artists = artists + ", " + artist;
                                    }
                                }
                                foreach (string performer in mp3fileperformers)
                                {
                                    if (performers == "")
                                    {
                                        performers = performer;
                                    }
                                    else
                                    {
                                        performers = performers + ", " + performer;
                                    }
                                }
                                foreach (string genre in mp3filegenres)
                                {
                                    if (genres == "")
                                    {
                                        genres = genre;
                                    }
                                    else
                                    {
                                        genres = genres + ", " + genre;
                                    }

                                }
                                if (artists == "")
                                {
                                    artists = performers;
                                }
                                connection.Open();
                                MySqlCommand command = connection.CreateCommand();
                                MySqlDataReader Reader;
                                command.CommandText = "SELECT uid FROM files WHERE name='" + MySqlEscape(fi.Name) + "' AND artists='" + MySqlEscape(artists) + "'";
                                Reader = command.ExecuteReader();
                                bool HasRows = Reader.HasRows;
                                Reader.Close();
                                connection.Close();
                                Console.WriteLine("HasRows = " + HasRows);
                                if (HasRows)
                                {
                                    connection.Open();
                                    string updatesql = "UPDATE files SET location='" + MySqlEscape(fi.DirectoryName) + "', genres='" + MySqlEscape(genres) + "', title='" + MySqlEscape(mp3filetitle) + "' WHERE name='" + MySqlEscape(fi.Name) + "' AND artists='" + MySqlEscape(artists) + "'";
                                    command = new MySqlCommand(updatesql, connection);
                                    command.ExecuteNonQuery();
                                    connection.Close();
                                    continue;
                                }
                                else
                                {
                                    connection.Open();
                                    string sql = "INSERT INTO files (name, location, genres, title, artists) VALUES ('" + MySqlEscape(fi.Name) + "','" + MySqlEscape(fi.DirectoryName) + "','" + MySqlEscape(genres) + "','" + MySqlEscape(mp3filetitle) + "','" + MySqlEscape(artists) + "')";
                                    command = new MySqlCommand(sql, connection);
                                    command.ExecuteNonQuery();
                                    connection.Close();
                                }
                                foreach (string singlegenre in mp3filegenres)
                                {
                                    if (singlegenre.Contains(',') || singlegenre.Contains('/') || singlegenre.Contains('|'))
                                    {
                                        char[] del = new char[]
                                        {
	                                        '/',
                                            '|',
	                                        ','
	                                    };
                                        string[] splitgenres = singlegenre.Split(del,StringSplitOptions.RemoveEmptyEntries);
                                        foreach (string splitgenre in splitgenres)
                                        {

                                            addgenres(splitgenre.Trim());
                                        }
                                    }
                                    else
                                    {
                                        addgenres(singlegenre);
                                    
                                    }
                                }
                            }
                            catch (TagLib.CorruptFileException e)
                            {
                                Console.WriteLine(e.Message);
                                continue;
                            }
                        }                        
                    }
                    catch (System.IO.FileNotFoundException e)
                    {
                        // If file was deleted by a separate application
                        //  or thread since the call to TraverseTree()
                        // then just continue.
                        Console.WriteLine(e.Message);
                        continue;
                    }
                }
                // Push the subdirectories onto the stack for traversal.
                // This could also be done before handing the files.
                foreach (string str in subDirs)
                    dirs.Push(str);
            }
        }
    }
}
