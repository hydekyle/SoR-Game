﻿using System.Collections;
using Assets.FantasyHeroes.Scripts;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class EquipManager : MonoBehaviour
{
    public static EquipManager Instance;

    public List<string> skinNames;
    public Dictionary<string, List<Sprite>> dicSkins;
    public List<Sprite> hairList, mouthList, eyebrownList, earsList, eyeList;
    [HideInInspector]
    public Sprite deadMouth, deadEyes;

    private void OnValidate()
    {
        ReadAllResources();
    }

    private void Awake()
    {
        if (Instance != null) Destroy(this.gameObject);
        Instance = this;
#if !UNITY_EDITOR
        // Cargar mapa después de esto
        OnValidate();
#endif
        deadMouth = mouthList.Find(mouth => mouth.name == "Crying");
        deadEyes = eyeList.Find(eye => eye.name == "Dead");
    }

    public void ReadAllResources()
    {
        string skinsRoute = "Sprites/Skins/" + SceneManager.GetActiveScene().name + "/";

        hairList = new List<Sprite>();
        mouthList = new List<Sprite>();
        eyebrownList = new List<Sprite>();
        earsList = new List<Sprite>();
        eyeList = new List<Sprite>();

        skinNames = new List<string>();
        dicSkins = new Dictionary<string, List<Sprite>>();

        var mouths = Resources.LoadAll("Sprites/BodyParts/Mouth", typeof(Sprite));
        var hairs = Resources.LoadAll("Sprites/BodyParts/Hair", typeof(Sprite));
        var eyebrowns = Resources.LoadAll("Sprites/BodyParts/Eyebrows", typeof(Sprite));
        var ears = Resources.LoadAll("Sprites/BodyParts/Ears", typeof(Sprite));
        var eyes = Resources.LoadAll("Sprites/BodyParts/Eyes/Basic", typeof(Sprite));

        foreach (var sprite in hairs) hairList.Add((Sprite)sprite);
        foreach (var sprite in mouths) mouthList.Add((Sprite)sprite);
        foreach (var sprite in eyebrowns) eyebrownList.Add((Sprite)sprite);
        foreach (var sprite in ears) earsList.Add((Sprite)sprite);
        foreach (var sprite in eyes) eyeList.Add((Sprite)sprite);

        // Leer sprite multiples
        var skins = Resources.LoadAll(skinsRoute, typeof(Texture2D));

        // Lee los conjuntos
        for (var x = 0; x < skins.Length; x++) skinNames.Add(skins[x].name);

        // Convertir skin multiple
        foreach (var skinName in skinNames)
        {
            List<Sprite> newList = new List<Sprite>();
            var skinSprites = Resources.LoadAll(skinsRoute + skinName, typeof(Sprite));
            foreach (var spriteObj in skinSprites) newList.Add((Sprite)spriteObj);
            dicSkins[skinName] = newList;
        }
    }

    void Equip(Character character, List<Sprite> skinSpriteList)
    {
        float colorValue = Random.Range(100f, 255f);
        Color skinColor = new Color(colorValue, colorValue, colorValue, 255f);
        SetSkinColor(character, skinColor);
        character.ArmorArmL = skinSpriteList[0];
        character.ArmorArmR = skinSpriteList[1];
        character.ArmorForearmL = skinSpriteList[2];
        character.ArmorForearmR = skinSpriteList[3];
        character.ArmorHandL = skinSpriteList[4];
        character.ArmorHandR = skinSpriteList[5];
        character.ArmorLeg = skinSpriteList[6];
        character.ArmorPelvis = skinSpriteList[7];
        character.ArmorShin = skinSpriteList[8];
        character.ArmorTorso = skinSpriteList[9];
        character.Mouth = GetOneRandom(mouthList);
        character.Hair = GetOneRandom(hairList);
        character.Eyebrows = GetOneRandom(eyebrownList);
        character.Ears = earsList.Find(ears => ears.name == "HumanEar");
        character.EarsRenderer.color = skinColor;
        character.Eyes = GetOneRandom(eyeList, 1);
        character.Initialize();
    }

    public void SetEquipment(Character character, string equipName)
    {
        List<Sprite> skinSpriteList = dicSkins[equipName];
        Equip(character, skinSpriteList);
    }

    public void SetRandomEquipment(Character character)
    {
        List<Sprite> skinSpriteList = GetRandomSkin();
        Equip(character, skinSpriteList);

    }

    private Sprite GetOneRandom(List<Sprite> spriteList)
    {
        return spriteList[Random.Range(0, spriteList.Count)];
    }

    private Sprite GetOneRandom(List<Sprite> spriteList, int startIndex)
    {
        return spriteList[Random.Range(startIndex, spriteList.Count)];
    }

    public void SetSkinColor(Character character, Color color)
    {
        character.HeadRenderer.color = color;
        character.ArmorArmLRenderer.color = color;
        character.ArmorPelvisRenderer.color = color;
        character.ArmorTorsoRenderer.color = color;
        foreach (var renderer in character.ArmorLegRenderers) renderer.color = color;
        foreach (var renderer in character.ArmorLegRenderers) renderer.color = color;
        foreach (var renderer in character.ArmorArmRRenderers) renderer.color = color;
        foreach (var renderer in character.ArmorForearmLRenderers) renderer.color = color;
        foreach (var renderer in character.ArmorForearmRRenderers) renderer.color = color;
        foreach (var renderer in character.ArmorHandLRenderers) renderer.color = color;
        foreach (var renderer in character.ArmorHandRRenderers) renderer.color = color;
        foreach (var renderer in character.ArmorShinRenderers) renderer.color = color;
    }

    List<Sprite> GetRandomSkin()
    {
        return dicSkins[skinNames[Random.Range(0, skinNames.Count)]];
    }

}