using System;
using System.Collections;
using System.Collections.Generic;
using MapMono;
using UnityEngine;

public class GameController : MonoBehaviour
{
    public StrategyCamera strategyCamera;
    public MapHandler mapHandler;
    public PlayerController playerController;
    public bool draggingMouse = false;
    public Vector3 dragStartPosition;
    public Vector3 dragCurrentPosition;
    
    public bool PauseGame = false;
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            TogglePause();
        }
        
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();
        }
        
        // if (Input.GetMouseButtonDown(0))
        // {
        //     draggingMouse = true;
        //     Plane plane = new Plane(Vector3.up, Vector3.zero);
        //
        //     Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        //     
        //     float entry;
        //
        //     if (plane.Raycast(ray, out entry))
        //     {
        //         dragStartPosition = ray.GetPoint(entry);
        //     }
        // }
        //
        // if (Input.GetMouseButton(0))
        // {
        //     Plane plane = new Plane(Vector3.up, Vector3.zero);
        //
        //     Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        //
        //     float entry;
        //
        //     if (plane.Raycast(ray, out entry))
        //     {
        //         dragCurrentPosition = ray.GetPoint(entry);
        //     }
        // }
        //
        // if (Input.GetMouseButtonUp(0))
        // {
        //     draggingMouse = false;
        //     
        // }

        if (Input.GetMouseButtonDown(0))
        {
            draggingMouse = true;
            Plane plane = new Plane(Vector3.up, Vector3.zero);
            
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            
            float entry;
            
            if (plane.Raycast(ray, out entry))
            {
                dragStartPosition = ray.GetPoint(entry);
            }
            
            mapHandler.HandleMouseClick(dragStartPosition);
        }

        if (Input.GetMouseButtonDown(1))
        {
            draggingMouse = true;
            Plane plane = new Plane(Vector3.up, Vector3.zero);
            
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            
            float entry;
            
            if (plane.Raycast(ray, out entry))
            {
                dragCurrentPosition = ray.GetPoint(entry);
            }
            mapHandler.HandleMouseRightClick(dragCurrentPosition);
        }
    }

    private void TogglePause()
    {
        PauseGame = !PauseGame;
        // if (PauseGame)
        // {
        //     Time.timeScale = 0f;
        // }
        // else
        // {
        //     Time.timeScale = 1f;
        // }
        
        strategyCamera.HandlePause(PauseGame);
        mapHandler.HandlePause(PauseGame);
        playerController.HandlePause(PauseGame);
        
    }
}
