using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Firebase.Auth;

public class FirebaseAuthentication : MonoBehaviour {

    public Text emailInput, passwordInput;
    public Text statusText;
    private string lastStatus = "";

    private void Start()
    {
        //lastStatus = "Welcome.\nPlease sign in above.";

    }

    //private void LateUpdate()
    //{
    //    if (lastStatus == statusText.GetComponent<Text>().text) return;

    //    statusText.GetComponent<Text>().text = lastStatus;
    //    lastStatus = statusText.GetComponent<Text>().text;

    //}

    public void Login() { 
    
        FirebaseAuth.DefaultInstance.SignInWithEmailAndPasswordAsync(emailInput.text,
                passwordInput.text).ContinueWith(( task => { 


                    if(task.IsCanceled) {

                        Firebase.FirebaseException e =
                        task.Exception.Flatten().InnerExceptions[0] as Firebase.FirebaseException;

                        GetErrorMessage((AuthError)e.ErrorCode);

                        return;

                    }

                    if (task.IsFaulted) {

                        Firebase.FirebaseException e =
                        task.Exception.Flatten().InnerExceptions[0] as Firebase.FirebaseException;

                        GetErrorMessage((AuthError)e.ErrorCode);

                        return;

                    }

                    if (task.IsCompleted) {
                        print("User is LOGGED IN");
                        lastStatus = "User is LOGGED IN: " + emailInput.text + " with pwd " + passwordInput.text;

                    }


                }));

    } // login

    public void Login_Anonymous() {

        FirebaseAuth.DefaultInstance.SignInAnonymouslyAsync().
            ContinueWith((task => {

                if (task.IsCanceled) {

                    Firebase.FirebaseException e =
                    task.Exception.Flatten().InnerExceptions[0] as Firebase.FirebaseException;

                    GetErrorMessage((AuthError)e.ErrorCode);

                    return;

                }

                if (task.IsFaulted) {

                    Firebase.FirebaseException e =
                    task.Exception.Flatten().InnerExceptions[0] as Firebase.FirebaseException;

                    GetErrorMessage((AuthError)e.ErrorCode);

                    return;

                }

                if (task.IsCompleted) {
                    print("User is LOGGED IN");
                    lastStatus = "Logged in " + emailInput.text + " with pwd " + passwordInput.text;
                }


            }));

    }

    public void RegisterUser() {
        lastStatus = "Registering user...";


        if (emailInput.text.Equals("") || passwordInput.text.Equals("")) {

            print("Please enter an email and password to register");
            lastStatus = "Please enter an email and password to register";

            return;

        }

        FirebaseAuth.DefaultInstance.CreateUserWithEmailAndPasswordAsync(emailInput.text,
                passwordInput.text).ContinueWith((task => { 

                    if(task.IsCanceled) {

                        Firebase.FirebaseException e =
                        task.Exception.Flatten().InnerExceptions[0] as Firebase.FirebaseException;

                        GetErrorMessage((AuthError)e.ErrorCode);

                        return;

                    }

                    if (task.IsFaulted) {

                        Firebase.FirebaseException e =
                        task.Exception.Flatten().InnerExceptions[0] as Firebase.FirebaseException;

                        GetErrorMessage((AuthError)e.ErrorCode);

                        return;

                    }

                    if(task.IsCompleted) {

                        print("Registration COMPLETE");
                        lastStatus = "Registration COMPLETE for " + emailInput.text + " with pwd " + passwordInput.text;


                    }

                }));

    }

    public void Logout() {

        if(FirebaseAuth.DefaultInstance.CurrentUser != null) {
        
            FirebaseAuth.DefaultInstance.SignOut();
            lastStatus = "Logged out user " + emailInput.text + " with pwd " + passwordInput.text;

        }

    }

    void GetErrorMessage(AuthError errorCode) {

        string msg = "";

        msg = errorCode.ToString();

        //switch(errorCode) {

        //    case AuthError.AccountExistsWithDifferentCredentials:
        //        // CODE
        //        break;

        //    case AuthError.MissingPassword:
        //        break;

        //    case AuthError.WrongPassword:
        //        break;

        //    case AuthError.InvalidEmail:
        //        break;

        //}


        //print(msg);
        lastStatus = msg;


    }

} // class







































