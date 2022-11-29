using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using TheoryTeam.PolymorphicGrid;
using UnityEngine.EventSystems;
using System.Linq;
using Cinemachine;

enum ControlState 
{
    free,
    friendlySelected,
}

public class InputManager : MonoBehaviour
{
    [SerializeField]
    private Cinemachine.CinemachineVirtualCamera gameplayCam;
    private Transform cameraFocus;

    [SerializeField]
    private float CameraRotationSpeed;
    [SerializeField]
    private float CameraTranspositionSpeed;

    [SerializeField]
    private GridMaster grid;
    [SerializeField]
    private GameObject tileCursor;
    [SerializeField]
    private GameObject actors;

    private CombatControls input;

    private Node highlightedNode = null;
    private GridObject selectedUnit = null;

    private ControlState controlState = ControlState.free;

    void Awake()
    {
        cameraFocus = gameplayCam.Follow;
        input = new CombatControls();
        tileCursor = Instantiate(tileCursor);
        tileCursor.SetActive(false);
        input.Generic.confirm.performed += OnSelect;
    }

    private void OnEnable()
    {
        input.Enable();
        input.Generic.Enable();
    }

    private void OnDisable()
    {
        input.Generic.Disable();
    }

    // Update is called once per frame
    void Update()
    {
        Vector2 direction = input.Generic.directions.ReadValue<Vector2>();
        float rotation = input.Generic.rotation.ReadValue<float>();

        Vector3 screenspaceCursor = input.Generic.cursor.ReadValue<Vector2>();
        Ray cursorRay = Camera.main.ScreenPointToRay(screenspaceCursor);
        RaycastHit cursorHit;
        Vector3? cursorPos = null;
        if (Physics.Raycast(cursorRay, out cursorHit))
        {
            cursorPos = cursorHit.point;
        }

        UpdatePos(direction);
        UpdateRot(rotation);
        UpdateTileCursor(cursorPos);
    }

    public void UpdatePos(Vector2 direction)
    {
        Vector3 screenRight = gameplayCam.transform.right;
        // make sure "right" is always horizontal in world space
        screenRight.y = 0;
        screenRight = screenRight.normalized;

        Vector3 screenDepth = Vector3.Cross(screenRight, Vector3.up);

        cameraFocus.position += (direction.x * screenRight + direction.y * screenDepth).normalized * (CameraTranspositionSpeed * Time.deltaTime);
    }

    public void UpdateRot(float rotation)
    {
        float angle = rotation * CameraRotationSpeed * Time.deltaTime;
        // manually set this deeply buried value to use rotation smoothing from orbital transposer
        gameplayCam.GetCinemachineComponent<CinemachineOrbitalTransposer>().m_XAxis.m_InputAxisValue = rotation;
        // cameraFocus.Rotate(xAngle: 0, yAngle: rotation * -CameraRotationSpeed * Time.deltaTime, zAngle: 0, Space.World);
    }

    public void UpdateTileCursor(Vector3? position)
    {
        // note: GetNode projects to grid, and non-terrain objects also stop the ray cast
        // so hovering over a unit will select the tile below it
        highlightedNode = (position == null) ? null : grid.GetNode(position.Value);


        tileCursor.SetActive(highlightedNode != null);
        if (highlightedNode != null)
        {
            tileCursor.transform.position = highlightedNode.WorldPosition;
        }
    }

    public void OnSelect(InputAction.CallbackContext context)
    {
        switch (controlState)
        {
            case ControlState.free:
                if (TrySelectUnit())
                    controlState = ControlState.friendlySelected;
                break;
            case ControlState.friendlySelected:
                if (TryMoveUnit())
                    controlState = ControlState.friendlySelected; // keep selected (redundant code)
                else if (!TrySelectUnit())
                    controlState = ControlState.free;
                break;
        }

        if (controlState != ControlState.friendlySelected && selectedUnit != null)
        {
            selectedUnit.Selected = false;
            selectedUnit = null;
        }

        //Debug.Log(controlState);
    }

    public bool TrySelectUnit()
    {
        // no cursor to select from
        if (!tileCursor.activeInHierarchy)
            return false;

        bool foundUnit = false;
        GridObject prevSelected = selectedUnit;
        // deselect any previously selected unit
        if (selectedUnit != null)
            selectedUnit.Selected = false;
        selectedUnit = null;

        foreach(GridObject actor in actors.GetComponentsInChildren<GridObject>())
        {
            // skip objects not at cursor
            if (actor.travelingNode != grid.GetNode(tileCursor.transform.position))
                continue;

            // second click should de-select
            if (actor == prevSelected)
                continue;

            // select a single new unit
            selectedUnit = actor;
            selectedUnit.Selected = true;
            foundUnit = true;
            break;
        }

        return foundUnit;
    }

    public bool TryMoveUnit()
    {
        if (!tileCursor.activeInHierarchy)
            return false;

        Node destination = grid.GetNode(tileCursor.transform.position);
        Node origin = selectedUnit.travelingNode;
        // enable clicking the unit again to deselect
        if (origin == destination)
            return false;

        // note: MUST copy path to a list or something something original IEnumerable gets updated during walk by next PathFinder operation sometimes
        List<Node> path = PathFinder.FindPath(destination, origin, grid)?.ToList();
        if (path == null)
            return false;
        selectedUnit.QueuePath(path);
        return true;
    }
}
