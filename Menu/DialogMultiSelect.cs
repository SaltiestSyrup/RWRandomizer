using System;
using System.Collections.Generic;
using Menu;
using UnityEngine;

namespace RainWorldRandomizer.Menu;

public class DialogMultiSelect : Dialog
{
    private List<SimpleButton> buttons;
    private List<Action> callbacks;
    
    public DialogMultiSelect(string description, ProcessManager manager, params (string, Action)[] options) 
        : base(description, manager)
    {
        Init(options);
    }

    public DialogMultiSelect(string description, Vector2 size, ProcessManager manager, params (string, Action)[] options) 
        : base(description, size, manager)
    {
        Init(options);
    }

    public override void Singal(MenuObject sender, string message)
    {
        base.Singal(sender, message);

        if (message.StartsWith("MULTISELECT_"))
        {
            if (int.TryParse(message.Split('_')[1], out int index))
            {
                callbacks[index]?.Invoke();
            }
            else
            {
                Plugin.Log.LogError("DialogMultiSelect failed to find callback");
            }
            manager.StopSideProcess(this);
        }
    }

    private void Init(params (string, Action)[] options)
    {
        buttons = [];
        callbacks = [];
        for (int i = 0; i < options.Length; i++)
        {
            callbacks.Add(options[i].Item2);
            SimpleButton button = new SimpleButton(this, pages[0], options[i].Item1, $"MULTISELECT_{i}",
                new Vector2(pos.x + (size.x * (i + 1) / (options.Length + 1) - 55f), pos.y + Mathf.Max(size.y * 0.04f, 7f)), 
                new Vector2(110f, 30f));
            buttons.Add(button);
            pages[0].subObjects.Add(button);
        }
    }
}