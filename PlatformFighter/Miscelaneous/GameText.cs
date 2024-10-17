using PlatformFighter.Rendering;

namespace PlatformFighter.Miscelaneous
{
    public static class GameText
    {
        public static MeasuredText PressButtonToJoin;
        public static MeasuredText PressButtonToStart;
        public static void Initialize()
        {
            PressButtonToJoin = new MeasuredText("Presiona C en teclado o X en gamepad para unirte\nPresiona B en teclado para añadir bot");
            PressButtonToStart = new MeasuredText("Presiona Z en teclado para iniciar juego!!!");
        }
    }
}