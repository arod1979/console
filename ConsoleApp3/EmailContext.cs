using RegistrationPractice.Entities;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace ConsoleApp3
{
    class EmailContext : DbContext
    {

        public EmailContext() : base("name=EmailConnection")
        {
            //Database.SetInitializer<EmailContext>(new DropCreateDatabaseIfModelChanges<EmailContext>());

        }
        public virtual DbSet<C__MigrationHistory> C__MigrationHistory { get; set; }
        public virtual DbSet<Email> Emails { get; set; }
        public virtual DbSet<EmailRecipients> EmailRecipients { get; set; }
        public virtual DbSet<HistoryID> HistoryIDs { get; set; }
        public virtual DbSet<FakeEmail> FakeEmails { get; set; }


        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
        }
    }

}
