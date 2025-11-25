using System.Collections; 
using UnityEngine;
using UnityEngine.UI;
using Firebase;
using Firebase.Auth;
using Firebase.Firestore;
using TMPro;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine.SceneManagement;

public class AuthManager : MonoBehaviour
{
    //Firebase variables
    [Header("Firebase")]
    public DependencyStatus dependencyStatus;
    public FirebaseAuth auth;    
    public FirebaseUser User;

    //Login variables
    [Header("Login")]
    public TMP_InputField userLoginField;
    public TMP_InputField passwordLoginField;
    public TMP_Text warningLoginText;
    public TMP_Text confirmLoginText;

    //Register variables
    [Header("Register Student")]
    public TMP_InputField firstnameRegisterField;
    public TMP_InputField lastnameRegisterField;
    public TMP_InputField usernameRegisterField;
    public TMP_InputField passwordRegisterField;
    public TMP_InputField passwordRegisterVerifyField;
    public TMP_Text warningRegisterText;

    [Header("Register Teacher")] 
    public TMP_InputField TusernameRegisterField;
    public TMP_InputField TfirstnameRegisterField;
    public TMP_InputField TlastnameRegisterField;
    public TMP_InputField TemailRegisterField;
    public TMP_InputField TpasswordRegisterField;
    public TMP_InputField TpasswordRegisterVerifyField;
    public Toggle isTeacherToggle;
    public TMP_Text TwarningRegisterText;

    // Firestore
    public FirebaseFirestore db;

    // Where we normalize usernames (trim + lowercase)
    private static string Key(string s) => string.IsNullOrWhiteSpace(s) ? "" : s.Trim().ToLowerInvariant();

    // Initialize Firebase
    void Awake()
    {
        //Check that all of the necessary dependencies for Firebase are present on the system
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task =>
        {
            dependencyStatus = task.Result;
            if (dependencyStatus == DependencyStatus.Available)
            {
                //If they are avalible Initialize Firebase
                InitializeFirebase();
            }
            else
            {
                Debug.LogError("Could not resolve all Firebase dependencies: " + dependencyStatus);
            }
        });
    }

    // Initialize the Firebase database and auth object
    private void InitializeFirebase()
    {
        Debug.Log("Setting up Firebase Auth/Firestore");
        auth = FirebaseAuth.DefaultInstance;
        db = FirebaseFirestore.DefaultInstance;
    }

    //Function for the login button
    public void LoginButton()
    {
        //Call the login coroutine passing the email and password
        StartCoroutine(Login(userLoginField.text, passwordLoginField.text));
    }
    //Function for the register button
    public void RegisterButton()
    {
        //Call the register coroutine passing the email, password, and username
        StartCoroutine(Register());
    }

    /* Coroutine for logging in a user */
    private IEnumerator Login(string identifier, string _password)
    {
        string input = identifier.Trim();
        string email = input;

        // If no '@', treat as username (teacher or student)
        if (!input.Contains("@"))
        {
            string key = Key(input); // normalize
            Debug.Log("[Login] Username lookup key: " + key); // debug log

            var nameMapTask = db.Collection("usernames").Document(key).GetSnapshotAsync(); // Lookup username
            yield return new WaitUntil(() => nameMapTask.IsCompleted); // wait for task to complete

            // Error handling
            if (nameMapTask.IsFaulted || nameMapTask.IsCanceled)
            {
                warningLoginText.text = "Network or permissions error.";
                yield break;
            }
            if (!nameMapTask.Result.Exists)
            {
                warningLoginText.text = "Username not found.";
                yield break;
            }

            // We saved email in the usernames map for both roles
            email = nameMapTask.Result.GetValue<string>("email");
        }

        var loginTask = auth.SignInWithEmailAndPasswordAsync(email, _password); // Attempt login
        yield return new WaitUntil(() => loginTask.IsCompleted); // wait for task to complete

        // Handle errors
        if (loginTask.Exception != null)
        {
            Debug.LogWarning($"Failed to login task with {loginTask.Exception}");
            var firebaseEx = loginTask.Exception.GetBaseException() as FirebaseException;
            string message = "Login Failed!";
            if (firebaseEx != null)
            {
                var errorCode = (AuthError)firebaseEx.ErrorCode;
                switch (errorCode)
                {
                    case AuthError.MissingEmail:    message = "Missing Email"; break;
                    case AuthError.MissingPassword: message = "Missing Password"; break;
                    case AuthError.WrongPassword:   message = "Wrong Password"; break;
                    case AuthError.InvalidEmail:    message = "Invalid Email"; break;
                    case AuthError.UserNotFound:    message = "Account does not exist"; break;
                }
            }
            warningLoginText.text = message;
            yield break;
        }

        // Login successful
        User = loginTask.Result.User;
        warningLoginText.text = "";
        confirmLoginText.text = "Logged In";
        Debug.LogFormat("User signed in successfully: {0} ({1})", User.DisplayName, User.Email);
        
        // Fetch the role from Firestore
        var userDocTask = db.Collection("users").Document(User.UserId).GetSnapshotAsync();
        yield return new WaitUntil(() => userDocTask.IsCompleted); // wait for task to complete

        // Error handling
        if (userDocTask.IsFaulted || userDocTask.IsCanceled)
        {
            Debug.LogError("Failed to fetch user data.");
            yield break;
        }

        // Check if document exists and get role
        if (userDocTask.Result.Exists)
        {
            string role = userDocTask.Result.GetValue<string>("role"); // get role
            Debug.Log("User role: " + role);

            // Redirect based on role
            if (role == "teacher")
                SceneManager.LoadScene("TeacherClassSelect");
            else
                SceneManager.LoadScene("StudentCharacterSelect");
        }
        // Document does not exist
        else
        {
            Debug.LogWarning("User document not found!");
        }
    }

    /* Coroutine for registering a new user */
    private IEnumerator Register()
    {
        bool isTeacher = isTeacherToggle != null && isTeacherToggle.isOn; // Check if registering as teacher

        // Separate flows for teacher and student
        if (isTeacher)
        {
            // === TEACHER FLOW ===
            string unameRaw = TusernameRegisterField.text;
            string unameKey = Key(unameRaw);              // normalized key for IDs
            string uname    = unameRaw.Trim();           // keep original-casing for displayName
            string fname    = TfirstnameRegisterField.text.Trim();
            string lname    = TlastnameRegisterField.text.Trim();
            string email    = TemailRegisterField.text.Trim();
            string pass     = TpasswordRegisterField.text;
            string pass2    = TpasswordRegisterVerifyField.text;

            // Basic validation
            if (string.IsNullOrEmpty(unameKey)) { TwarningRegisterText.text = "Missing Username"; yield break; }
            if (string.IsNullOrEmpty(email))    { TwarningRegisterText.text = "Missing Email";    yield break; }
            if (string.IsNullOrEmpty(pass))     { TwarningRegisterText.text = "Missing Password"; yield break; }
            if (pass != pass2)                  { TwarningRegisterText.text = "Passwords do not match"; yield break; }

            // Enforce unique username (via usernames collection)
            Debug.Log("[Register] Checking usernames/" + unameKey); // debug
            var nameCheckTask = db.Collection("usernames").Document(unameKey).GetSnapshotAsync(); // check username
            yield return new WaitUntil(() => nameCheckTask.IsCompleted); // wait for task to complete
            
            // check if username exists
            if (nameCheckTask.Result.Exists)
            {
                TwarningRegisterText.text = "Username already taken";
                yield break;
            }

            // Create user with email and password
            var regTask = auth.CreateUserWithEmailAndPasswordAsync(email, pass);
            yield return new WaitUntil(() => regTask.IsCompleted);
            
            // Handle errors
            if (regTask.Exception != null)
            {
                HandleAuthError(regTask.Exception, TwarningRegisterText);
                yield break;
            }

            var user = regTask.Result.User; // newly created user

            // Set display name to username
            var profileTask = user.UpdateUserProfileAsync(new UserProfile { DisplayName = uname });
            yield return new WaitUntil(() => profileTask.IsCompleted);

            // Write profile (users/{uid}) and username mapping (usernames/{username})
            var profile = new Dictionary<string, object>
            {
                { "uid", user.UserId },
                { "username", uname },  // keep display version
                { "firstName", fname },
                { "lastName", lname },
                { "email", email },
                { "role", "teacher" }
            };

            // Write to users collection
            Debug.Log("[Register] Writing users/" + user.UserId);
            var userDocTask = db.Collection("users").Document(user.UserId).SetAsync(profile);

            // Write to usernames collection
            Debug.Log("[Register] Writing usernames/" + unameKey); // use normalized key
            var mapDocTask  = db.Collection("usernames").Document(unameKey).SetAsync(new Dictionary<string, object> // mapping doc
            {
                { "uid", user.UserId },
                { "email", email },
                { "role", "teacher" }
            });
            // wait for both writes to complete
            yield return new WaitUntil(() => userDocTask.IsCompleted && mapDocTask.IsCompleted);

            TwarningRegisterText.text = "";
            
            // implement back to login
            // UIManager.instance.LoginScreen();
        }
        else
        {
            // === STUDENT FLOW ===
            string unameRaw = usernameRegisterField.text;
            string unameKey = Key(unameRaw);             // normalized key for IDs/emails
            string uname    = unameRaw.Trim();           // keep original-casing for display
            string fname    = firstnameRegisterField.text.Trim();
            string lname    = lastnameRegisterField.text.Trim();
            string pass     = passwordRegisterField.text;
            string pass2    = passwordRegisterVerifyField.text;

            // Basic validation
            if (string.IsNullOrEmpty(unameKey)) { warningRegisterText.text = "Missing Username"; yield break; }
            if (string.IsNullOrEmpty(pass))     { warningRegisterText.text = "Missing Password"; yield break; }
            if (pass != pass2)                  { warningRegisterText.text = "Passwords do not match"; yield break; }

            // Enforce unique username
            Debug.Log("[Register] Checking usernames/" + unameKey); // debug
            var nameCheckTask = db.Collection("usernames").Document(unameKey).GetSnapshotAsync(); // check username
            yield return new WaitUntil(() => nameCheckTask.IsCompleted);
            if (nameCheckTask.Result.Exists)
            {
                warningRegisterText.text = "Username already taken";
                yield break;
            }

            // Use a synthetic email for students since no email field is provided
            string syntheticEmail = unameKey + "@students.example";

            var regTask = auth.CreateUserWithEmailAndPasswordAsync(syntheticEmail, pass); // create user
            yield return new WaitUntil(() => regTask.IsCompleted);

            // Handle errors
            if (regTask.Exception != null)
            {
                HandleAuthError(regTask.Exception, warningRegisterText);
                yield break;
            }

            var user = regTask.Result.User; // newly created user

            // Set display name to username
            var profileTask = user.UpdateUserProfileAsync(new UserProfile { DisplayName = uname });
            yield return new WaitUntil(() => profileTask.IsCompleted);

            // Write profile (users/{uid}) and username mapping (usernames/{username})
            var profile = new Dictionary<string, object>
            {
                { "uid", user.UserId },
                { "username", uname },  // keep display version
                { "firstName", fname },
                { "lastName", lname },
                { "email", syntheticEmail }, // synthetic
                { "role", "student" }
            };

            Debug.Log("[Register] Writing users/" + user.UserId);
            var userDocTask = db.Collection("users").Document(user.UserId).SetAsync(profile); // write profile

            Debug.Log("[Register] Writing usernames/" + unameKey); // use normalized key
            var mapDocTask  = db.Collection("usernames").Document(unameKey).SetAsync(new Dictionary<string, object> // mapping doc
            {
                { "uid", user.UserId },
                { "email", syntheticEmail },
                { "role", "student" }
            });

            yield return new WaitUntil(() => userDocTask.IsCompleted && mapDocTask.IsCompleted); // wait for writes

            warningRegisterText.text = ""; // clear warning

            // implement back to login
            // UIManager.instance.LoginScreen();
        }
    }

    /* Handle authentication errors and display appropriate messages */
    private void HandleAuthError(System.AggregateException ex, TMP_Text uiLabel)
    {
        Debug.LogWarning($"Auth error: {ex}");
        var firebaseEx = ex.GetBaseException() as FirebaseException;
        string msg = "Register failed";
        if (firebaseEx != null)
        {
            var code = (AuthError)firebaseEx.ErrorCode;
            switch (code)
            {
                case AuthError.MissingEmail:       msg = "Missing Email"; break;
                case AuthError.MissingPassword:    msg = "Missing Password"; break;
                case AuthError.WeakPassword:       msg = "Weak Password"; break;
                case AuthError.EmailAlreadyInUse:  msg = "Email already in use"; break;
                case AuthError.InvalidEmail:       msg = "Invalid Email"; break;
                default:                           msg = code.ToString(); break;
            }
        }
        uiLabel.text = msg;
    }
}
