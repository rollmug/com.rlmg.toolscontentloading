namespace rlmg.Tools.ContentLoading.Examples
{
    using System;
    using UnityEngine;

    // test

    /// <summary>
    /// Represents a serializable container for Pokémon data from GraphQL response.
    /// </summary>
    /// <remarks>To use with JsonUtility, classes like this must be explicitly marked Serializable.</remarks>
    [Serializable]
    public class PokemonGraphDataWrapper
    {
        public PokemonData data;
    }

    /// <summary>
    /// Represents a serializable container for Pokémon data from REST GET response.
    /// </summary>
    [Serializable]
    public class PokemonRESTDataWrapper
    {
        public PokemonSpecies[] results;
    }

    /// <summary>
    /// Represents serializable Pokémon data.
    /// </summary>
    /// <remarks>To use with JsonUtility, classes like this must be explicitly marked Serializable.</remarks>
    [Serializable]
    public class PokemonData
    {
        public PokemonSpecies[] pokemonspecies;
    }

    /// <summary>
    /// Represents serializable data for a single Pokémon species item.
    /// </summary>
    /// <remarks>To use with JsonUtility, classes like this must be explicitly marked Serializable.</remarks>
    [Serializable]
    public class PokemonSpecies
    {
        /// <summary>
        /// Pokédex ID. Bulbasaur is 1.
        /// </summary>
        public int id;

        /// <summary>
        /// Name of Pokémon. 
        /// </summary>
        public string name;

        /// <summary>
        /// API endpoint for this Pokémon species.
        /// </summary>
        public string url;

        /// <summary>
        /// Flavor texts for the Pokémon.
        /// The text is different across game versions so that's why this is a list, because multiple
        /// flavor texts are assigned to a single Pokémon.
        /// </summary>
        public PokemonSpeciesFlavorText[] pokemonspeciesflavortexts;

        /// <summary>
        /// Field to which the ContentLoader will assign a Pokémon image.
        /// A NonSerialized property like this is useful for accessing images closely related to specific data classes.
        /// </summary>
        [NonSerialized]
        public Texture2D Texture;

        /// <summary>
        /// Field to which the ContentLoader will assign where it has cached a local copy of the texture image.
        /// A NonSerialized property like this is useful for accessing images closely related to specific data classes.
        /// </summary>
        [NonSerialized]
        public string TextureCachePath;
    }

    /// <summary>
    /// Represents localized flavor text for a Pokémon species. 
    /// </summary>
    [Serializable]
    public class PokemonSpeciesFlavorText
    {
        public string flavor_text;
    }

}