# Unity-GoodInputManager  
Attach to an empty object  
Drug&Drop public methods to the array in the editor, when you push the key, the method will invoke
![image](https://user-images.githubusercontent.com/101827492/216754996-143fba0c-582c-405c-8734-3a407c64d45b.png)

# What is good?
* You can register method on editor. 
* High-Level-Support of n-times-pushes  
You can do just like "shortly walk (1st push) before run (2nd push).
![sasa_20230204_161340](https://user-images.githubusercontent.com/101827492/216756425-631cf050-f2e0-4272-acf4-3f42198ab7c1.gif)
* Integration of key, button, function pushes(such as TriggerEvent)
* You can register by code too

# Why use?
* Integrate all keys to single place, so can handle key configration easily.
* Enable edit keys on editor
* For high-level n-times-pushes

# How to use
## Register Action
### From Editor
Drag & Drop public method  
![sasa_20230204_165049](https://user-images.githubusercontent.com/101827492/216755894-2627dd95-7542-4dce-a908-dfe1bfab99a2.gif)
  
### From Script
Use InputManager.RegisterAction() to register, InputManager.RemoveAction() to unregister.
  
### Meanings of the parameters
#### SimpleAction? SeveralPushesAction?
If you want n-pushes, Use SeveralPushesAction. If not, use SimpleAction.  

#### DontWait?
If true(checke), the action start without waiting next action.  
The action will be canceled on the next push

## Push key not from key/buttons
Use Push()  
You can register this to EventTrigger
