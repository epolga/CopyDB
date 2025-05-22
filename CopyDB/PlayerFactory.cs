using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CopyDB
{
    interface IPlayerFactory
    {
        IPlayer? CreatePlayer(string name); 
    }

    interface IPlayer 
    {
        
        void Play();
    }

    public class RegularPlayer : IPlayer
    {
        public void Play()
        {
            //Play
        }
    }
    public class ProPlayer : IPlayer
    {
        public void Play()
        {
            //Play
        }
    }

    class PlayerFactory : IPlayerFactory
    {
        public IPlayer? CreatePlayer(string name)
        {
            IPlayer? player = null;
            switch (name) {
                case "Pro":
                    player = new ProPlayer();
                    break;
                case "Regular":
                    player = new ProPlayer();
                    break;
            }
            return player;
        }
    }
}
