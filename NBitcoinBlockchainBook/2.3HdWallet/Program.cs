﻿using System;
using NBitcoin;

//c Add project. I got problem to be resolved in previous example. I should acheive 2 requirements. 1.Prevent outdated backups, 2.Delegating key/address generation to an untrusted peer. A "Deterministic" wallet would fix the backup problem. With such a wallet, I would have to save only the seed. From this seed, I can generate the same series of private keys over and over. From the master key, I can generate the new keys.

namespace HdWallet
{
    internal class Program
    {
        private static void Main()
        {
            RandomUtils.Random = new UnsecureRandom();

            //Create a masterKey.
            ExtKey masterKey = new ExtKey();
            Console.WriteLine("Master key : " + masterKey.ToString(Network.Main));

            //Create 6 keys based on the masterKey.
            for (int i = 0; i < 5; i++)
            {
                ExtKey key = masterKey.Derive((uint)i);
                Console.WriteLine("Key " + i + " : " + key.ToString(Network.Main));
            }
            //I only need to save the masterKey, since I can generate the same suite of private keys over and over.
            //As I can see, these keys are ExtKey, not Key used to. However, this should not stop me since I have the real private key inside.


            //I can go back from a Key to an ExtKey by supplying the Key and the ChainCode to the ExtKey constructor.
            //Create extKey.
            ExtKey extKey = new ExtKey();
            //Get ChainCode from extKey.
            byte[] chainCode = extKey.ChainCode;
            //Get PrivateKey from extKey.
            Key key2 = extKey.PrivateKey;
            //Supply PrivateKey and ChainCode to ExtKey constructor.
            ExtKey newExtKey = new ExtKey(key2, chainCode);


            //The base58 type which is equivalent of ExtKey is called BitcoinExtKey. How can I solve the second problem, delegating key/address creation process to an untrusted peer which can potentially be hacked(like a payment server)? The trick is that I can "neuter" my master key, and then I have a public version of the master key(without private key). From this neutered version, a third party can generate public keys without knowing the private key.
            //Neuter masterKey and then I have a public version of the masterKey masterPubKey which doesn't contain privateKey.
            ExtPubKey masterPubKey = masterKey.Neuter();

            //Create 5 pubkeys from masterPubKey
            for (int i = 0; i < 5; i++)
            {
                ExtPubKey pubkey = masterPubKey.Derive((uint)i);
                Console.WriteLine("PubKey " + i + " : " + pubkey.ToString(Network.Main));
            }

            //So imagine the scenario that my payment server generates pubkey1, I can get the corresponding private key with my private master key.
            masterKey = new ExtKey();
            masterPubKey = masterKey.Neuter();

            //The third party untrusted peer payment server generates pubkey1 from masterPubKey.
            ExtPubKey pubkey1 = masterPubKey.Derive((uint)1);

            //You get the private key of pubkey1
            ExtKey key1 = masterKey.Derive((uint)1);

            //Check it is legit
            Console.WriteLine("Generated address : " + pubkey1.PubKey.GetAddress(Network.Main));
            Console.WriteLine("Expected address : " + key1.PrivateKey.PubKey.GetAddress(Network.Main));
            //Generated address: 1Jy8nALZNqpf4rFN9TWG2qXapZUBvquFfX
            //Expected address:	 1Jy8nALZNqpf4rFN9TWG2qXapZUBvquFfX
            //ExtPubKey is similar to ExtKey except that it holds a PubKey and not a Key.



            //I have seen how Deterministic keys solve 2 problems. It's time to examine about what the "hierarchical” is for.
            //In the previous exercise, I've seen that by combining master key + index, I could generate another key.
            //I call this process "Derivation", the master key is the "parent key", and any generated keys are called "child keys".
            //However, I can also derivate children from the "child key". This is what the "hierarchical” stands for. I can say more generally, Parent key + KeyPath => Child key.

            //Just suppose the scenario that there is parent key, "Parent".
            //And there are child keys derived from "Parent", Child(1),Child(2),Child(3),Child(4). 
            //And There are child keys derived from Child(1), Child(1, 1), Child(1, 2).
            ExtKey parent = new ExtKey();
            ExtKey child11 = parent.Derive(1).Derive(1);

            //Or above code can be expressed in this way.
            //parent = new ExtKey();
            //child11 = parent.Derive(new KeyPath("1/1"));
            Console.WriteLine(child11);

            //Remember that Ancestor ExtKey + KeyPath = Child ExtKey.
            //This process works the same for ExtPubKey.
            //Why do I need hierarchical keys? It's because it might be a nice way to classify the type of my keys for multiple accounts. This point is more than on BIP44. It also permits segmenting account rights across an organization. Let's suppose the scenario that I'm a CEO of a company. I want to control over all wallets. In this point, I don't want the Accounting department to spend the money from the Marketing department. For implementing this constraint, the first idea would be to generate one hierarchy for each department.
            //CEO Key->Child Keys : Marketing(0), Accounting(0).
            //Marketing(0)->Child Keys:Marketing(0, 1), Marketing(0, 2).
            //Accounting(0)->Child Keys:Accounting(0, 2), Accounting(0, 2).

            //However, in such case, one problem comes that Accounting and Marketing would be able to recover the CEO's private key. In above code, I defined such child keys as non-hardened.
            //Parent ExtPubKey + Child ExtKey(non hardened) => Parent ExtKey.

            ExtKey ceoKey = new ExtKey();
            Console.WriteLine("CEO: " + ceoKey.ToString(Network.Main));
            //Note the hardened: false
            ExtKey accountingKey = ceoKey.Derive(0, hardened: false);

            ExtPubKey ceoPubkey = ceoKey.Neuter();

            //Recover ceo key with accounting private key and ceo public key
            ExtKey ceoKeyRecovered = accountingKey.GetParentExtKey(ceoPubkey);
            Console.WriteLine("CEO recovered: " + ceoKeyRecovered.ToString(Network.Main));
            //CEO: xprv9s21ZrQH143K2XcJU89thgkBehaMqvcj4A6JFxwPs6ZzGYHYT8dTchd87TC4NHSwvDuexuFVFpYaAt3gztYtZyXmy2hCVyVyxumdxfDBpoC
            //CEO recovered: xprv9s21ZrQH143K2XcJU89thgkBehaMqvcj4A6JFxwPs6ZzGYHYT8dTchd87TC4NHSwvDuexuFVFpYaAt3gztYtZyXmy2hCVyVyxumdxfDBpoC

            //In other simply words, a non-hardened key can "climb" the hierarchy.
            //Non-hardened keys should only be used for categorizing accounts which belong to a point of single control.
            //So, in this case, the CEO should create a hardened key(by setting named argument hardened: true), so that the accounting department won't be able to climb the hierarchy upward.



            //Same process to above code except for hardened:true.
            //ExtKey ceoKey = new ExtKey();
            //Console.WriteLine("CEO: " + ceoKey.ToString(Network.Main));
            //Derive accountKeys from ceoKey, but accountKeys are hardened so that they can't climb hierarchy towards ceoKey.
            //ExtKey accountingKeyHardened = ceoKey.Derive(0, hardened: true);

            //ExtPubKey ceoPubkey = ceoKey.Neuter();
            ////At this point, it'll be crashed with this climbing attempt.
            //ExtKey ceoKeyRecovered = accountingKey.GetParentExtKey(ceoPubkey); 


            //I can also create hardened keys via the ExtKey.Derivate(KeyPath), by using an apostrophe(') after a child’s index such as "1/2/3'"
            var nonHardened = new KeyPath("1/2/3");
            var hardened = new KeyPath("1/2/3'");
            Console.WriteLine(nonHardened);
            Console.WriteLine(hardened);


            //Let's suppose that the Accounting department generates 1 parent key for each customer, and a child for each of the customer's payments.
            //As the CEO, I want to spent the money on one of these addresses.
            //Here is the code for that.
            ceoKey = new ExtKey();
            //Hardened. Can't climb hierarchy upward.
            string accounting = "1'";
            int customerId = 5;
            int paymentId = 50;
            KeyPath path = new KeyPath(accounting + "/" + customerId + "/" + paymentId);
            //Path will be "1'/5/50"
            ExtKey paymentKey = ceoKey.Derive(path);
            Console.WriteLine(paymentKey);



            //I've seen how to generate HD keys. However, what if I want an easy way to transmit such a key by telephone or hand writing?
            //Cold wallets such as Trezor generate the HD keys from a sentence that can be easily memorized or written down. They call such a sentence "the seed" or "mnemonic”. And it can eventually be protected by a password or a PIN.
            //The thing that I use to generate my "easy to memorize and write" sentence is called a Wordlist.
            //Wordlist+mnemonic+password=>HD Root key.
            Mnemonic mnemo = new Mnemonic(Wordlist.English, WordCount.Twelve);
            ExtKey hdRoot1 = mnemo.DeriveExtKey("my password");
            Console.WriteLine(mnemo);
            Console.WriteLine(hdRoot1);

            //Now, if I have the mnemonic and the password, I can recover the hdRoot key.
            mnemo = new Mnemonic("minute put grant neglect anxiety case globe win famous correct turn link", Wordlist.English);
            ExtKey hdRoot2 = mnemo.DeriveExtKey("my password");
            Console.WriteLine(hdRoot2);
        }
    }
}