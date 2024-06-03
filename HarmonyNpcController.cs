using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using Unity.AI.Navigation;
using System.Linq;

/*
 * HarmonyNpcController - AddOn for Harmony Connector, which allows it to control a ProjectP AI NPC.
 * Version: 0.1
 * Compatibility: Harmony Link v0.2.0 onwards.
 * 
 * This module simulates basic AI NPC movement, and is soon to be upgraded with real movement information.
 */
public class HarmonyNpcController : CustomBehaviour
{
    // Unity Components
    public float maxDistance = 10;
    NpcDesc npcDesc;
    Animator animator;
    NavMeshAgent nav;
    Transform head;

    // Reference to Harmony Connector
    public HarmonyConnector harmonyConnector;

    // Sync Strings are used across Unity Plugins and Components to exchange information (interal, global state machine). Also, the value gets replicated across all connected game clients.
    // npcName gets updated by the Connector with the name of the Entity received via ChatHistory Event
    public SyncString npcName;

    void Start()
    {

        npcName = new SyncString(this, "npcName");
        animator = GetComponent<Animator>();
        nav = gameObject.GetComponent<NavMeshAgent>();
        if(nav == null) nav = gameObject.AddComponent<NavMeshAgent>();
        nav.speed = 1;
        nav.stoppingDistance = 2;
        Vector3 csize = CalculateRecursiveBounds(gameObject).size;
        nav.height = csize.y;
        nav.radius = nav.height / 10.0f;
        NavMeshObstacle navObstacle = gameObject.GetComponent<NavMeshObstacle>();
        if(navObstacle == null) navObstacle = gameObject.AddComponent<NavMeshObstacle>();
        navObstacle.height = nav.height;
        navObstacle.radius = nav.radius;
        navObstacle.center = new Vector3(0, csize.y / 2, 0);
        npcDesc = GetComponent<NpcDesc>();
        if(npcDesc == null) npcDesc = gameObject.AddComponent<NpcDesc>();
        head = animator.GetBoneTransform(HumanBodyBones.Head);
        nav.ResetPath();
        nav.isStopped = false;

        NavMeshSurface closestNavMeshSurface = GetClosestNavMeshSurface();
        if (closestNavMeshSurface != null) nav.agentTypeID = closestNavMeshSurface.agentTypeID;

        SetVoiceCallback(GetPlayer().id, CallbackVoice);
    }


    void Update()
    {
        if(harmonyConnector == null) harmonyConnector = gameObject.GetComponent<HarmonyConnector>();

        if (Camera.main == null) return;
        if ((Vector3.Distance(Camera.main.transform.position, transform.position) < 2 && animator.GetBool("Walking")))
        {
            TakeOwnership();
            animator.SetBool("Walking", false);
            nav.isStopped = true;
        }

        else if (IsOwned() && Vector3.Distance(Camera.main.transform.position, transform.position) < 2)
        {
            FaceTarget(Camera.main.transform.position);
        }

        else if (IsOwned())
        {
            animator.SetBool("Walking", true);
            nav.isStopped = false;
            if (!nav.pathPending)
            {
              if (nav.remainingDistance <= nav.stoppingDistance)
                {
                    if (!nav.hasPath || nav.velocity.sqrMagnitude == 0f)
                    {
                        Vector3 rndPoint = GetRandomPoint(new Vector3(0, 0, 0), maxDistance);
                        nav.SetDestination(rndPoint);
                    }
                }
            }
        }

        else if (!IsOwned())
        {
            nav.isStopped = true;
        }

        GameObject go = GetPlayer().baseObject;
        if (go != null)
        {
            CustomBehaviour cb = go.GetComponent<CustomBehaviour>(); 
            SyncString PlayerChatMessage = new SyncString(cb, "chatMessageForNpc_" + npcName.val);
            if (PlayerChatMessage.dirty && !PlayerChatMessage.initial)
            {
                SetChatAsync(PlayerChatMessage.val);
                PlayerChatMessage.dirty = false;
            }
        }
 
        if(harmonyConnector.chatResult != null)
        {
            Debug.Log("chatResult");
            TakeOwnership();
            SyncString npcChatMessage;
            npcChatMessage = new SyncString(this, "npcChatMessage_" + npcName.val);
            npcChatMessage.val = harmonyConnector.chatResult;                   
            harmonyConnector.SendTtsGenerateSpeechAsync(harmonyConnector.chatResult);
            harmonyConnector.chatResult = null;
        }
        
        if(npcName.dirty)
        {
            npcDesc.npcName = npcName.val;
        }
    }

    private void FaceTarget(Vector3 destination)
    {
        Vector3 lookPos = destination - transform.position;
        lookPos.y = 0;
        Quaternion rotation = Quaternion.LookRotation(lookPos);
        transform.rotation = Quaternion.Slerp(transform.rotation, rotation, 0.1f);
    }

    public static Vector3 GetRandomPoint(Vector3 center, float maxDistance)
    {
        Vector3 randomPos = Random.insideUnitSphere * maxDistance + center;
        NavMeshHit hit;
        NavMesh.SamplePosition(randomPos, out hit, maxDistance, NavMesh.AllAreas);
        return hit.position;
    }

    enum NpcGesture
    {
        idle, cry, kiss, death, kick, jump, laugh, punch, yelling, dance, shy
    }

    IEnumerator OnCompleteAnimation()
    {
        while (animator.GetCurrentAnimatorStateInfo(0).IsName("Idle") ||
                animator.GetCurrentAnimatorStateInfo(0).IsName("Walking"))
        {
            yield return null;
        }

        animator.SetInteger("Gesture", (int)NpcGesture.idle);
    }


    public void SetChatAsync(string text)
    {        
        harmonyConnector.UserUttrance(text);
    }

    public void CallbackVoice(string base64)
    {   
        harmonyConnector.SendAudioDataAsync(base64);
    }

    public Bounds CalculateRecursiveBounds(GameObject go)
    {
        SkinnedMeshRenderer[] mfs = go.GetComponentsInChildren<SkinnedMeshRenderer>();

        if (mfs.Length > 0)
        {
            Bounds b = mfs[0].bounds;
            for (int i = 1; i < mfs.Length; i++)
            {
                b.Encapsulate(mfs[i].bounds);
            }
            return b;
        }
        else return new Bounds();
    }

    private NavMeshSurface GetClosestNavMeshSurface()
    {
        NavMeshSurface closestSurface = FindObjectsOfType<NavMeshSurface>()
            .OrderBy(surface => Vector3.Distance(transform.position, surface.transform.position))
            .FirstOrDefault();
        return closestSurface;
    }

}