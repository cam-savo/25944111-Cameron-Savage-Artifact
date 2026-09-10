using UnityEngine;

public class ShipTile : MonoBehaviour
{
    public int index;
    public string TileName;
    public int ID;
    public Ship_Sockets faceForward = Ship_Sockets.Blank;
    public Ship_Sockets faceBackward = Ship_Sockets.Blank;
    public Ship_Sockets faceLeft = Ship_Sockets.Blank;
    public Ship_Sockets faceRight = Ship_Sockets.Blank;
    public Ship_Sockets faceUp = Ship_Sockets.Blank;
    public Ship_Sockets faceDown = Ship_Sockets.Blank;

    //Checks all sockets to see if a match is found
    public bool socketMatch(Ship_Sockets socket, Ship_Sockets socketToMactch)
    {
        bool match = false;

        switch (socket)
        {
            case Ship_Sockets.Star:
                if (socketToMactch == Ship_Sockets.Star) match = true;
                break;
            case Ship_Sockets.Square:
                if (socketToMactch == Ship_Sockets.Square) match = true;
                break;
            case Ship_Sockets.Triangle:
                if (socketToMactch == Ship_Sockets.Triangle) match = true;
                break;
            case Ship_Sockets.Circle:
                if (socketToMactch == Ship_Sockets.Circle) match = true;
                break;
            case Ship_Sockets.Cross:
                if (socketToMactch == Ship_Sockets.Cross) match = true;
                break;
            case Ship_Sockets.Line:
                if (socketToMactch == Ship_Sockets.Blank || socketToMactch == Ship_Sockets.Line) match = true;
                break;
            case Ship_Sockets.Blank:
                if (socketToMactch == Ship_Sockets.Line || socketToMactch == Ship_Sockets.Blank) match = true;
                break;
            case Ship_Sockets.None:
                break;
        }

        return match;
    }

}
