using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bee.Db.UnitTests
{
    [Collection("Initialize")]
    public class SessionRepositoryTests
    {
        [Fact]
        public void CreateSession()
        {            
            var repo = new SessionRepository();
            var sessionUse = repo.CreateSession("001");
        }
    }
}
