using UnityEditor;
using UnityEngine;

// Configura al importar los audios de Resources/Audio:
// - Música/ambiente (Resources/Audio/*): pesan varios MB → Vorbis en streaming, sin cargarlos enteros.
// - Efectos (Resources/Audio/UI/*, Resources/Audio/Jugador/*...): clips cortos → cargados en memoria para que suenen al instante.
public class ImportarAudioAmbiente : AssetPostprocessor
{
    const string Raiz = "Assets/_Project/Resources/Audio/";

    void OnPreprocessAudio()
    {
        if (!assetPath.StartsWith(Raiz)) return;

        var importador = (AudioImporter)assetImporter;
        var config = importador.defaultSampleSettings;
        // Todo lo que esté en una subcarpeta (UI/, Jugador/...) son efectos cortos
        bool esCorto = assetPath.Substring(Raiz.Length).Contains("/");

        if (esCorto)
        {
            importador.forceToMono = true;
            importador.loadInBackground = false;
            config.loadType = AudioClipLoadType.DecompressOnLoad;
            config.compressionFormat = AudioCompressionFormat.ADPCM;
            config.preloadAudioData = true;
        }
        else
        {
            importador.forceToMono = false;
            importador.loadInBackground = true;
            config.loadType = AudioClipLoadType.Streaming;
            config.compressionFormat = AudioCompressionFormat.Vorbis;
            config.quality = 0.7f;
            config.preloadAudioData = false;
        }
        importador.defaultSampleSettings = config;
    }
}
