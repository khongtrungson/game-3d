using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class can_throw_grenade : MonoBehaviour
{
    // Start is called before the first frame update
	private GameObject[] bullet_kit;
		private bool reloading_range=false;
    public GameObject[] grenade_present;
    private GameObject player;
    public GameObject grenade_checking;
    public bool can_throw=false;

    // Update is called once per frame
    void Update()
    {
        grenade_present= GameObject.FindGameObjectsWithTag("grenade");
        player= GameObject.FindGameObjectWithTag("Player");
        if(grenade_present.Length>0){
            can_throw=true;
        }
        else{
            can_throw=false;
        }


        if (Input.GetKeyDown(KeyCode.R))
        {
            RefillGrenades();
        }
    }

    /// <summary>
    /// Nạp đầy 8 quả lựu đạn khi người chơi đứng gần hộp tiếp tế bullet_kit (khoảng cách < 2.5m)
    /// </summary>
    public void RefillGrenades()
    {
        if (player == null) player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        bullet_kit = GameObject.FindGameObjectsWithTag("bullet_kit");
        if (bullet_kit == null) return;

        foreach (var kit in bullet_kit)
        {
            if (kit == null) continue;
            float dis = Vector3.Distance(player.transform.position, kit.transform.position);
            if (dis < 2.5f)
            {
                grenade_present = GameObject.FindGameObjectsWithTag("grenade");
                if (grenade_present != null)
                {
                    foreach (var gre in grenade_present)
                    {
                        if (gre != null) Destroy(gre);
                    }
                }

                var prefab = Resources.Load("grenade_checking") as GameObject;
                if (prefab != null)
                {
                    for (int i = 0; i < 8; i++)
                    {
                        Instantiate(prefab);
                    }
                }
                break;
            }
        }
    }
    public void destro_one(){
        grenade_present= GameObject.FindGameObjectsWithTag("grenade");
        if(grenade_present.Length>0){
            foreach(var gre in grenade_present){
                Destroy(gre);
                break;
            }
        }
    }
}
