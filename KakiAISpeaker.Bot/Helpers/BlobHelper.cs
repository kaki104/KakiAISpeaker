using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace KakiAISpeaker.Bot.Helpers
{
    /// <summary>
    ///     블롭 헬퍼
    /// </summary>
    public class BlobHelper
    {
        /// <summary>
        ///     컨테이너 이름은 소문자, 하이픈, 숫자로만 지정 가능
        /// </summary>
        private const string VOICE_CONTAINER = "voice-container";

        private CloudBlobContainer _cloudBlobContainer;
        private bool _isInit;

        private CloudStorageAccount _storageAccount;
        private readonly string _storageConnectionString;

        public BlobHelper(IConfiguration configuration)
        {
            _storageConnectionString = configuration.GetSection("storageConnectionString").Value;
            Init();
        }

        /// <summary>
        ///     초기화
        /// </summary>
        private async void Init()
        {
            if (_isInit) return;

            //컨넥션 스트링 확인
            if (!CloudStorageAccount.TryParse(_storageConnectionString, out _storageAccount)) return;
            try
            {
                //스토레이지 계정을 이용해서 Blob클라이언트 생성
                var cloudBlobClient = _storageAccount.CreateCloudBlobClient();
                //음성 저장용 컨테이너 생성
                _cloudBlobContainer = cloudBlobClient.GetContainerReference(VOICE_CONTAINER);

                if (await _cloudBlobContainer.CreateIfNotExistsAsync(BlobContainerPublicAccessType.Blob, 
                    new BlobRequestOptions(), new OperationContext())) return;
                _isInit = true;
            }
            catch (StorageException se)
            {
                Debug.WriteLine(se.RequestInformation.HttpStatusMessage);
                throw;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        /// <summary>
        ///     블럭 블롭 업로드
        /// </summary>
        /// <returns></returns>
        public async Task<string> UploadBlockBlobAsync(string blobName, Stream sourceStream)
        {
            try
            {
                //블롭컨테이너에 블럭 만들기
                var cloudBlockBlob = _cloudBlobContainer.GetBlockBlobReference(blobName);

                var ms = new MemoryStream();
                await sourceStream.CopyToAsync(ms);

                ms.Position = 0;
                //파일 업로드
                //await cloudBlockBlob.UploadFromFileAsync(data.ToString());
                await cloudBlockBlob.UploadFromStreamAsync(ms);

                await ms.FlushAsync();
                //블럭 uri반환
                return cloudBlockBlob.SnapshotQualifiedUri.ToString();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }



        /// <summary>
        ///     Delete Block
        /// </summary>
        /// <param name="blobName"></param>
        /// <returns></returns>
        public async Task DeleteBlockBlobAsync(string blobName)
        {
            try
            {
                var cloudBlockBlob = _cloudBlobContainer.GetBlockBlobReference(blobName);
                await cloudBlockBlob.DeleteIfExistsAsync();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        public async Task<BlobResultSegment> ListBlobAsync()
        {
            try
            {
                var list = await _cloudBlobContainer.ListBlobsSegmentedAsync(new BlobContinuationToken());
                return list;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
    }
}