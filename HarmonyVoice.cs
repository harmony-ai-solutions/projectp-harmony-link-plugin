using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

/*
 * HarmonyVoice - Simple AddOn for Harmony Connector, which allows it to render AI Voiceover Events
 * Author: katoki (ProjectP) and RuntimeRacer (Project Harmony.AI)
 * Version: 0.1
 * Compatibility: Harmony Link v0.2.0 onwards.
 */
public class HarmonyVoice : CustomBehaviour
{
    // Unity Components
    AudioSource audioSource = null;    
    Transform head;

    // Sync Strings are used across Unity Plugins and Components to exchange information (interal, global state machine)
    // npcAudioBase64 gets updated by the Connector each time there is a new Audio event received
    SyncString npcAudioBase64;

    void Start()
    {
        // Initializes all parts of the module
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        npcAudioBase64 = new SyncString(this, "npcAudioBase64");
        head = GetComponent<Animator>().GetBoneTransform(HumanBodyBones.Head);
        head.GetComponent<AudioSource>().Stop();
    }

    void Update()
    {
        // Check if audio data was updated on each tick
        if (npcAudioBase64.dirty && !npcAudioBase64.initial)
        {
            npcAudioBase64.dirty = false;
            StartCoroutine(PlayVoice());
        }
    }

    IEnumerator PlayVoice()
    {
        AudioSource audioSource = head.GetComponent<AudioSource>();
        audioSource.Stop();
        audioSource.clip = Base64StringToAudioClip(npcAudioBase64.val);
        audioSource.Play();
        yield return null;
    }

}