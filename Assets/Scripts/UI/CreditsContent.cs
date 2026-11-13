using System;

// Closing text supplied by the team. Keep spelling, order and paragraphs intact.
public static class CreditsContent
{
    public const string Title = "AETHERION: APOCALYPSE";
    public enum Style { Heading, Body, Name, Emphasis, Signature, Final }
    public readonly struct Block
    {
        public readonly string Text;
        public readonly Style Tone;
        public Block(string text, Style tone) { Text = text; Tone = tone; }
    }
    public static readonly Block[] Blocks = {
        new Block("Obrigado por jogar!", Style.Heading),
        new Block("Toda grande jornada começa com um sonho. A nossa começou com a vontade de transformar ideias, criatividade e conhecimento em uma aventura inesquecível.", Style.Body),
        new Block("Aetherion: Apocalypse foi desenvolvido como um projeto acadêmico por alunos do Centro Universitário UNICEPLAC, representando o esforço, a dedicação e o trabalho em equipe de todos os envolvidos.", Style.Body),
        new Block("Mais do que um jogo, este projeto representa os desafios que superamos, os conhecimentos que adquirimos e a satisfação de ver nossas ideias ganharem vida.", Style.Body),
        new Block("Equipe de Desenvolvimento", Style.Heading),
        new Block("Bianca Xavier de Oliveira", Style.Name),
        new Block("Gabriel Lucas Alves da Silva", Style.Name),
        new Block("Isabela Rosa dos Santos Gontijo", Style.Name),
        new Block("Matheus Nascimento Cardoso", Style.Name),
        new Block("Samer Osama Mohammad Taleeb", Style.Name),
        new Block("Thiago Costa Renovato", Style.Name),
        new Block("Wesley Thiago Matias Xavier", Style.Name),
        new Block("Agradecimento Especial", Style.Heading),
        new Block("Professor Weverson Garcia Medeiros", Style.Emphasis),
        new Block("Nosso sincero agradecimento pela orientação, pelo apoio e pelos conhecimentos compartilhados durante o desenvolvimento deste projeto.", Style.Body),
        new Block("Agradecemos também à UNICEPLAC pela oportunidade de colocar em prática os conhecimentos adquiridos ao longo de nossa formação.", Style.Body),
        new Block("Uma última mensagem...", Style.Heading),
        new Block("A você, jogador, nosso muito obrigado por dedicar seu tempo a conhecer o mundo de Aetherion.", Style.Body),
        new Block("Cada batalha enfrentada, cada desafio superado e cada vitória conquistada fizeram parte desta jornada.", Style.Body),
        new Block("Assim como os heróis de Aetherion, acreditamos que, mesmo diante dos maiores desafios, a dedicação, a coragem e a esperança podem transformar qualquer realidade.", Style.Body),
        new Block("Obrigado por fazer parte da nossa história.", Style.Emphasis),
        new Block("Que a luz de Aetherion jamais se apague.", Style.Signature),
        new Block("FIM", Style.Final)
    };
    public static string FullText => Title + "\n\n" + string.Join("\n\n", Array.ConvertAll(Blocks, block => block.Text));
}
