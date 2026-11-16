using UnityEngine;

// el orden de los arreglos de efectos es comida, electricidad, radiacion y descanso
public class Dilemma
{
    public string titulo;
    public string texto;
    public string dato;
    public string opcionA;
    public string opcionB;
    public float[] efectoA;
    public float[] efectoB;

    public Dilemma(string titulo, string texto, string dato, string opcionA, float[] efectoA, string opcionB, float[] efectoB)
    {
        this.titulo = titulo;
        this.texto = texto;
        this.dato = dato;
        this.opcionA = opcionA;
        this.efectoA = efectoA;
        this.opcionB = opcionB;
        this.efectoB = efectoB;
    }
}

public static class DilemmaList
{
    static Dilemma[] todos = new Dilemma[]
    {
        new Dilemma("SOLAR STORM ALERT",
            "DONKI, the NASA space weather database, reports a solar particle event heading for the Moon.",
            "Solar particle events are bursts of fast protons from the Sun. Water is great at blocking them.",
            "Pour water reserves into the storm shelter walls.", new float[] { -15, 0, 25, 0 },
            "Run the magnetic shield from the batteries.", new float[] { 0, -25, 20, 0 }),

        new Dilemma("MOON DUST",
            "The habitat roof is thin. Moon dust, called regolith, could be piled on top as a blanket.",
            "Regolith helps block radiation, but the dust is sharp and sticks to everything.",
            "Crew shovels regolith over the roof.", new float[] { 0, 0, 20, -15 },
            "Skip it and let the crew rest.", new float[] { 0, 0, -10, 10 }),

        new Dilemma("LONG LUNAR NIGHT",
            "The sun is gone for about two weeks. The solar panels make no power at all.",
            "One lunar night lasts around 14 Earth days, so bases need batteries.",
            "Turn off the greenhouse lights.", new float[] { -20, 20, 0, 0 },
            "Keep the plants happy and drain the batteries.", new float[] { 10, -20, 0, 0 }),

        new Dilemma("CLOGGED AIR FILTER",
            "The carbon dioxide scrubber is full. The air is getting stale.",
            "Crew breathe out carbon dioxide, so scrubbers must clean the air all day long.",
            "Shut down the lab to give the scrubber more power.", new float[] { 20, -10, 0, 0 },
            "Clean every filter by hand.", new float[] { 15, 0, 0, -15 }),

        new Dilemma("COSMIC RAY WEEK",
            "Galactic cosmic rays are higher than usual. They come from far away in the galaxy.",
            "Cosmic rays are very energetic and hard to stop. Thick walls only help a little.",
            "Sleep in the water walled bunk.", new float[] { -10, 0, 0, 15 },
            "Use the time to fix the solar panels.", new float[] { 0, 15, -10, 0 }),

        new Dilemma("DUSTY SOLAR PANELS",
            "Dust covers the panels and the power is falling.",
            "Lunar dust is electric and clings to metal, so cleaning it is a real job.",
            "Brush the panels by hand outside.", new float[] { 0, 20, 0, -15 },
            "Wait and save your energy.", new float[] { 0, -10, 0, 5 }),

        new Dilemma("WATER LEAK",
            "A pipe in the greenhouse is leaking water drop by drop.",
            "Every drop counts in space. Real stations recycle most of their water.",
            "Patch it with the power tools.", new float[] { 15, -10, 0, 0 },
            "Use the crew water ration to save the plants.", new float[] { 10, 0, 0, -10 }),

        new Dilemma("TIRED CREW",
            "Everyone has been working for days. Mistakes are starting to happen.",
            "Rest matters in space. A tired crew makes more mistakes with heavy machines.",
            "Give the crew a day off.", new float[] { -10, -5, 0, 25 },
            "Keep working, the plants need care.", new float[] { 10, 0, 0, -15 })
    };

    public static Dilemma ObtenerAleatorio()
    {
        return todos[Random.Range(0, todos.Length)];
    }
}
