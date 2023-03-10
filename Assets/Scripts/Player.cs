using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    public static Player instance;

    [SerializeField] private Transform ship;
    [SerializeField] private Transform band;
    [SerializeField] private float walkSpeed;
    [SerializeField] private float sailSpeed;
    [SerializeField] private float rotateSpeed;
    [SerializeField] private float cameraFollowSpeed;
    private Vector3 shipTarget;
    private Vector3 bandTarget;
    private Vector3 lastMovement;
    private Vector3 shipLookDirection = Vector3.forward;
    private Vector3 bandLookDirection = Vector3.forward;
    [SerializeField] private Transform cameraT;

    private bool inShip = true;

    [System.NonSerialized] public Transform bay;
    public DifficultyOptions<int> startingPirates;

    [System.NonSerialized] public PirateBand pirateBand;

    [System.NonSerialized] public bool canMouseMove;

    [System.NonSerialized] public bool hasMoved;

    private static bool hasCompass;
    public Transform compass;

    // Start is called before the first frame update
    void Start() {
        instance = this;
    }

    public void Play(int difficulty) {
        Vector3 offset = new Vector3(GameManager.size/2, 0, GameManager.size/2);
        ship.position -= offset;
        band.position -= offset;
        cameraT.position -= offset;
        shipTarget = ship.position;
        bandTarget = band.position;
        pirateBand = band.GetChild(0).GetComponent<PirateBand>();
        pirateBand.AddPeople(startingPirates.Get());
        hasCompass = Settings.compass.Get();
    }

    float Sign(float f) {
        if (f == 0) return 0;
        return Mathf.Sign(f);
    }

    // Update is called once per frame
    void Update()
    {
        if (!GameManager.hasStarted) return;

        float x = (Input.GetKey(Settings.right.Get()) ? 1 : 0)-(Input.GetKey(Settings.left.Get()) ? 1 : 0);
        float z = (Input.GetKey(Settings.up.Get()) ? 1 : 0)-(Input.GetKey(Settings.down.Get()) ? 1 : 0);

        if (x == 0 && z == 0) {
            x = Sign(Input.GetAxis("Horizontal"));
            z = Sign(Input.GetAxis("Vertical"));
        }
        if (canMouseMove && Input.GetMouseButton(0) && x == 0 && z == 0) {
            Vector3 mouseOffset = Camera.main.ScreenToViewportPoint(Input.mousePosition)-new Vector3(0.5f,0.63f);
            mouseOffset = new Vector3((mouseOffset.x/Screen.height)*Screen.width, mouseOffset.y, 0);
            float mag = mouseOffset.magnitude;
            if (mag > 0.075f && mag < 0.6f) {
                mouseOffset.Normalize();
                x = Sign(Mathf.Round(mouseOffset.x));
                z = Sign(Mathf.Round(mouseOffset.y));
            }
        }
        if (!canMouseMove && Input.GetMouseButtonUp(0) && GameManager.playing) canMouseMove = true;

        float distance = (band.position-bandTarget).magnitude;
        if (((distance < 0.1f) || (((lastMovement.x!=x && z==0 && x!=0) || (lastMovement.z!=z && x==0 && z!=0)) && distance < 0.5f)) && (x != 0 || z != 0)) {
            hasMoved = true;
            if (inShip && bay != null && bandTarget-bay.position == Vector3.up && -bay.right == new Vector3(x,0,z)) {
                inShip = false;
                bay.parent.GetComponent<Island>().EnterIsland(pirateBand);
            }
            Vector3 offset = inShip ? new Vector3(0,-0.125f,0) : new Vector3(0,0.125f,0);
            if (Physics.Raycast(bandTarget + offset, new Vector3(x,0,0), 1)) {
                x = 0; 
            } 
            if (Physics.Raycast(bandTarget + offset, new Vector3(0,0,z), 1)) {
                z = 0;
            }
            if (x != 0 && z != 0) {
                if (lastMovement.x == 0) z = 0;
                else x = 0;
            }
            Vector3 movement = new Vector3(x,0,z);
            if (!inShip) {
                if (!Physics.Raycast(bandTarget+offset+movement, Vector3.down, 0.15f)) {
                    movement = Vector3.zero;
                }
            }
            if (movement != Vector3.zero) {
                bandTarget = new Vector3(bandTarget.x+movement.x, 1, bandTarget.z+movement.z);
                if (inShip) {
                    shipLookDirection = movement;
                }
                else if (bandTarget == ship.position) {
                    bay.parent.GetComponent<Island>().ExitIsland(pirateBand);
                    inShip = true;
                }
                bandLookDirection = movement;
                GameManager.instance.UpdateIslands();
            }
            if (inShip) {
                shipTarget = bandTarget;
            }
            lastMovement = movement;
        }

        if (hasCompass) {
            if (inShip || Vector3.Distance(band.position, ship.position) < 0.5) {
                compass.gameObject.SetActive(false);
            } else {
                compass.gameObject.SetActive(true);
                compass.GetChild(0).rotation = Quaternion.LookRotation((band.position-ship.position), Vector3.up);
                compass.GetChild(1).rotation = Quaternion.LookRotation((band.position-bay.parent.position)-new Vector3(0,1,0), Vector3.up);
                compass.GetChild(1).gameObject.SetActive(!Island.current.collected);
            }
        }

        band.position = Vector3.MoveTowards(band.position, bandTarget, Time.deltaTime*(inShip ? sailSpeed : walkSpeed));
        ship.position = Vector3.MoveTowards(ship.position, shipTarget, Time.deltaTime*sailSpeed);
        cameraT.position = Vector3.MoveTowards(cameraT.position, band.position, Vector3.Distance(cameraT.position, band.position)*Time.deltaTime*cameraFollowSpeed);

        ship.rotation = Quaternion.Lerp(ship.rotation, Quaternion.LookRotation(shipLookDirection, Vector3.up), rotateSpeed*Time.deltaTime);
        pirateBand.transform.rotation = Quaternion.Lerp(pirateBand.transform.rotation, Quaternion.LookRotation(bandLookDirection, Vector3.up), rotateSpeed*Time.deltaTime);
    }

    public static Vector3 getPosition() {
        return instance.bandTarget;
    }

    public static void ToggleCompass(bool to) {
        hasCompass = to;
    }
}
