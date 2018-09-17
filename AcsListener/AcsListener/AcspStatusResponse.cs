using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace AcsListener
{
    public enum GeneralStatusResponseKey : Byte
    {
        RrpSuccessful = 0,      // Request successfully processed
        RrpFailed = 1,          // ACS unable to process Request
        RrpInvalid = 2,         // Invalid parameter or Request structure
        AcsBusy = 3,            // ACS too busy to process Request
        LeaseTimeout = 4,       // Lease not valid for last transaction
        PlayoutIdMismatch = 5,  // RPL and CSP Playout ID mismatch
        GeneralError = 6,       // General Error response
        RecoverableError = 7,   // Recoverable Error
        RplError = 8,           // Error parsing RPL
        ResourceError = 9,      // Error parsing resource file
        Processing = 10,        // ACS busy getting or interpreting resources
        ReservedRangeMin= 11,   // UINT8 values 11-255 are reserved
        ReservedRangeMax= 255,  // UINT8 values 11-255 are reserved
    }

    public class AcspStatusResponse
    {
        private GeneralStatusResponseKey _key;    // SMPTE 430-10 specifications call for this to be UInt8, which is a "Byte" in C#
        private AcspBerLength _length;
        private string _message;

        public AcspStatusResponse(Byte[] bytePack)
        {
            if (bytePack.Length < 5)
            {
                throw new ArgumentOutOfRangeException("Error:  expected at least a 5-byte array to be passed to the Status Response constructor");
            }

            Byte[] lengthArray = new Byte[4];

            int i = 0;  // index for Array.Copy
            _key = (GeneralStatusResponseKey)bytePack[0];
            i = i + 1;  // Adding 1 byte for _key
            Array.Copy(bytePack, i, lengthArray, 0, lengthArray.Length);
            i = i + lengthArray.Length;  // Adding more bytes for the BER length array

            _length = new AcspBerLength(lengthArray);

            Byte[] messageArray = new byte[_length.Length];
            Array.Copy(bytePack, i, messageArray, 0, messageArray.Length);
            i = i + messageArray.Length;  // Adding more bytes for the message string

            _message = Encoding.UTF8.GetString(messageArray);
        }

        public string KeyAsString
        {
            get
            {

                switch (Key)
                {
                    case GeneralStatusResponseKey.AcsBusy:
                        return "AcsBusy";
                    case GeneralStatusResponseKey.GeneralError:
                        return "GeneralError";
                    case GeneralStatusResponseKey.LeaseTimeout:
                        return "LeaseTimeout";
                    case GeneralStatusResponseKey.PlayoutIdMismatch:
                        return "PlayoutIdMismatch";
                    case GeneralStatusResponseKey.Processing:
                        return "Processing";
                    case GeneralStatusResponseKey.RecoverableError:
                        return "RecoverableError";
                    case GeneralStatusResponseKey.ResourceError:
                        return "ResourceError";
                    case GeneralStatusResponseKey.RplError:
                        return "RplError";
                    case GeneralStatusResponseKey.RrpFailed:
                        return "RrpFailed";
                    case GeneralStatusResponseKey.RrpInvalid:
                        return "RrpInvalid";
                    case GeneralStatusResponseKey.RrpSuccessful:
                        return "RrpSuccessful";
                    default:
                        return "";
                }
            }
        }

        public GeneralStatusResponseKey Key
        {
            get
            {
                return _key;
            }
        }

        public string Message
        {
            get
            {
                if (_message == null)
                {
                    throw new NullReferenceException("Error:  Status Response Message is null referenced");
                }
                return _message;
            }
        }

        /*
        public void Decode(NetworkStream stream)
        {
            Byte[] header = new Byte[5];

            stream.Read(header, 0, header.Length);
            _key = header[0];



         
        } */
    }
}
