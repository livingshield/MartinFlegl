using System;
using System.Data.SqlClient;
using FluentFTP;

namespace ftp_db_test
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("--- Testování připojení ---");

            // 1. Test FTP
            TestFtp();

            Console.WriteLine();

            // 2. Test DB
            TestDb();
            
            Console.WriteLine("\nTestování dokončeno.");
        }

        static void TestFtp()
        {
            Console.WriteLine("[FTP] Pokus o připojení...");
            string ftpHost = "windows11.aspone.cz";
            string ftpUser = "EkoBio.org_lordkikin";
            string ftpPass = "Brzsilpot7!";

            try
            {
                using (var client = new FtpClient(ftpHost, ftpUser, ftpPass))
                {
                    client.AutoConnect();
                    
                    if (client.IsConnected)
                    {
                        Console.WriteLine("[FTP] ÚSPĚCH: Připojeno k FTP serveru.");
                        
                        // Zkusit vypsat kořenovy adresář
                        Console.WriteLine("[FTP] Nalezené soubory/složky v kořenovém adresáři:");
                        foreach (var item in client.GetListing())
                        {
                            Console.WriteLine($"      - {item.Name} ({item.Type})");
                        }
                    }
                    else
                    {
                        Console.WriteLine("[FTP] CHYBA: Nepodařilo se připojit (IsConnected == false).");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FTP] CHYBA: Výjimka při připojování:\n{ex.Message}");
            }
        }

        static void TestDb()
        {
            Console.WriteLine("[DB] Pokus o připojení k databázi...");
            string sqlConnectionString = "Server=sql8.aspone.cz;Database=db4937;User Id=db4937;Password=lordkikin;Encrypt=False";

            try
            {
                using (SqlConnection connection = new SqlConnection(sqlConnectionString))
                {
                    connection.Open();
                    Console.WriteLine("[DB] ÚSPĚCH: Připojení k MS SQL databázi bylo úspěšné.");
                    
                    // Zkusit jednoduchy query
                    using (SqlCommand command = new SqlCommand("SELECT @@VERSION", connection))
                    {
                        string version = (string)command.ExecuteScalar();
                        Console.WriteLine($"[DB] Verze SQL serveru: {version.Split('\n')[0]}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DB] CHYBA: Výjimka při připojování k databázi:\n{ex.Message}");
            }
        }
    }
}
